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

@customElement("sf-field-config-property")
export class FieldConfigPropertyElement extends UmbElementMixin(LitElement) {
  @property({ type: Object })
  public set field(value: FormPropertyBackofficeModel | undefined) {
    this._field = value;
    if (this.Element) {
      this.Element.field = value!;
    }
    this.observePropertyView();
  }
  public get field() {
    return this._field;
  }
  private _field?: FormPropertyBackofficeModel;

  @state()
  public Element?: IFormFieldConfigElement;

  private observePropertyView() {
    if (!this._field) {
      return;
    }

    new UmbExtensionsManifestInitializer(
      this,
      umbExtensionsRegistry,
      "formFieldConfig",
      null,
      (documents) => {
        documents.forEach((document) => {
          const manifest =
            document.manifest as unknown as FormFieldConfigManifest;

          if (
            !manifest ||
            manifest.propertyTypeAlias !== this._field!.propertyEditor
          ) {
            return;
          }
          this._gotEditorUI(manifest);
        });
      },
    );
  }

  private async _gotEditorUI(
    manifest?: FormFieldConfigManifest | null,
  ): Promise<void> {
    if (!manifest) {
      return;
    }

    const el = await createExtensionElement(manifest);
    if (el) {
      this.Element = el;
      this.Element.field = this._field!;

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
