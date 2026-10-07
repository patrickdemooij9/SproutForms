import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";
import {
  customElement,
  property,
  state,
  when,
} from "@umbraco-cms/backoffice/external/lit";
import { html, LitElement } from "lit";
import { umbExtensionsRegistry } from "@umbraco-cms/backoffice/extension-registry";
import {
  createExtensionElement,
  UmbExtensionsManifestInitializer,
} from "@umbraco-cms/backoffice/extension-api";
import { FormPropertyBackofficeModel } from "../../api";
import {
  FormFieldConfigManifest,
  IFormFieldConfigElement,
} from "../../manifests/formFieldConfigManifest";
import { UmbChangeEvent } from "@umbraco-cms/backoffice/event";
import { FormFieldDto } from "../../models";

@customElement("sf-field-config-property")
export class FieldConfigPropertyElement extends UmbElementMixin(LitElement) {
  @property({ type: Object })
  public set field(value: FormPropertyBackofficeModel | undefined) {
    this._field = value;
    if (this.Element) {
      this.Element.field = value!;
    }
    // The parent passes a new object on every render; only another editor needs looking up, and a new element
    if (value && value.propertyEditor !== this.#editorAlias) {
      this.observePropertyView();
    }
  }
  public get field() {
    return this._field;
  }
  private _field?: FormPropertyBackofficeModel;

  @property({ type: Object })
  public set formField(value: FormFieldDto | undefined) {
    this._formField = value;
    if (this.Element) {
      this.Element.formField = value;
    }
  }
  public get formField() {
    return this._formField;
  }
  private _formField?: FormFieldDto;

  @state()
  public Element?: IFormFieldConfigElement;

  // The property editor the element is for, the initializer looking up its manifest, and the manifest the element was made from
  #editorAlias?: string;
  #editorInitializer?: { destroy(): void };
  #elementManifestAlias?: string;

  private observePropertyView() {
    if (!this._field) {
      return;
    }

    const editorAlias = this._field.propertyEditor;
    this.#editorAlias = editorAlias;
    this.#editorInitializer?.destroy();
    this.#elementManifestAlias = undefined;
    this.Element = undefined;

    this.#editorInitializer = new UmbExtensionsManifestInitializer(
      this,
      umbExtensionsRegistry,
      "formFieldConfig",
      (manifest) =>
        (manifest as unknown as FormFieldConfigManifest).propertyTypeAlias === editorAlias,
      (documents) => {
        const manifest = documents[0]?.manifest as unknown as FormFieldConfigManifest | undefined;
        // The registry reports again when any extension is added; the element already made for this manifest stays
        if (manifest && manifest.alias !== this.#elementManifestAlias) {
          this._gotEditorUI(manifest, editorAlias);
        }
      },
    );
  }

  private async _gotEditorUI(
    manifest: FormFieldConfigManifest,
    editorAlias: string,
  ): Promise<void> {
    this.#elementManifestAlias = manifest.alias;

    const el = await createExtensionElement(manifest);
    // The field may have moved on to another editor while this one was loading
    if (el && editorAlias === this.#editorAlias) {
      this.Element = el;
      this.Element.field = this._field!;
      this.Element.formField = this._formField;

      this.Element.addEventListener("change", () => {
        this.dispatchEvent(new UmbChangeEvent());
      });
      /*this._element.addEventListener("change", () => {
        this._field = {
          ...this._field!,
          userValue: this._element!.value,
        };
        this.dispatchEvent(new UmbPropertyValueChangeEvent());
      });

      this._element.value = this.field?.userValue;
      if (this.field?.editConfig) {
        this._element.config = new UmbPropertyEditorConfigCollection(
          Object.entries(this.field?.editConfig).map((item) => ({
            alias: item[0],
            value: item[1],
          }))
        );
      }*/
    }
  }

  render() {
    return when(
      this.Element,
      () => this.Element,
      () => html`
        <umb-property
          alias=${this.field!.alias}
          label=${this.field!.displayName}
          description=""
          property-editor-ui-alias=${this.field!.propertyEditor}
          .appearance=${{
            labelOnTop: true,
          }}
        ></umb-property>
      `,
    );
  }
}
