import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";
import { css, html, LitElement } from "lit";
import { property, state } from "lit/decorators.js";
import {
  repeat,
  customElement,
  when,
} from "@umbraco-cms/backoffice/external/lit";
import {
  UmbPropertyDatasetElement,
  UmbPropertyValueData,
} from "@umbraco-cms/backoffice/property";
import { FieldConfigPropertyElement } from "./fieldEditors/fieldConfigProperty.element";

import "./fieldEditors/fieldConfigProperty.element";
import "./fieldEditors/fieldConditionsEditor.element";
import {
  FormFieldDto,
  FormFieldTypeDto,
  FormDefinitionDto,
  FormDefinitionTypeDto,
} from "../models";

export class FieldChangeEvent extends Event {
  static readonly TYPE = "field-change";
  field!: Partial<FormFieldDto>;

  constructor(detail?: Partial<FormFieldDto>) {
    super(FieldChangeEvent.TYPE, { bubbles: true, composed: true });
    if (detail) this.field = detail;
  }
}

@customElement("sf-inspector-field-type")
export default class FormInspectorFieldTypeElement extends UmbElementMixin(
  LitElement,
) {
  @property({ type: Object })
  public set field(value: FormFieldDto) {
    this._field = value;
    this.setValues();
  }
  public get field() {
    return this._field;
  }
  private _field!: FormFieldDto;

  @property({ type: Object })
  public fieldType!: FormFieldTypeDto;

  @property({ type: Array })
  public fields: FormDefinitionDto["fields"] = [];

  @property({ type: Object })
  public set formType(value: FormDefinitionTypeDto | undefined) {
    this._formType = value;
    this.setValues();
  }
  public get formType() {
    return this._formType;
  }
  private _formType?: FormDefinitionTypeDto;

  // The properties the form's type adds to this field's type, if it extends it
  get #extension() {
    return this.formType?.fieldExtensions.find(
      (it) => it.fieldTypeAlias === this.field?.fieldTypeAlias,
    );
  }

  // The field's extension settings, with defaults for any it doesn't have yet
  #getExtensionValues(): Record<string, unknown> {
    const values: Record<string, unknown> = {};
    this.#extension?.properties.forEach((prop) => {
      values[prop.alias] = this.field.extension?.[prop.alias] ?? prop.value ?? null;
    });
    return values;
  }

  @state()
  private _values: Array<UmbPropertyValueData> = [];

  @state()
  private _activeTab = "general";

  private setValues() {
    if (!this.field) {
      this._values = [];
      return;
    }
    this._values = [
      {
        alias: "label",
        value: this.field!.label,
      },
      {
        alias: "required",
        value: this.field!.required,
      },
      {
        alias: "alias",
        value: this.field!.alias,
      },
    ];
    Object.entries(this.field!.configuration).forEach(([key, value]) => {
      this._values.push({
        alias: key,
        value: value,
      });
    });
    Object.entries(this.#getExtensionValues()).forEach(([key, value]) => {
      this._values.push({
        alias: "extension-" + key,
        value: value,
      });
    });
  }

  #onPropertyDataChange(e: Event) {
    const value = (e.target as UmbPropertyDatasetElement).value;

    const updatedField: Partial<FormFieldDto> = {
      id: this.field.id
    };
    updatedField.configuration = structuredClone(this.field?.configuration);
    const extension = this.#extension ? this.#getExtensionValues() : undefined;
    value.forEach((item) => {
      if (item.alias.startsWith("extension-")) {
        const actualAlias = item.alias.replace("extension-", "");
        if (extension && Object.keys(extension).includes(actualAlias)) {
          extension[actualAlias] = item.value;
        }
      } else if (item.alias == "label") {
        updatedField.label = item.value as string;
      } else if (item.alias == "required") {
        updatedField.required = item.value as boolean;
      } else if (item.alias == "alias") {
        updatedField.alias = item.value as string;
      } else {
        if (Object.keys(updatedField.configuration!).includes(item.alias)) {
          updatedField.configuration![item.alias] =
            item.value?.toString() ?? "";
        }
      }
    });

    if (extension) {
      updatedField.extension = extension;
    }

    const fieldChangeEvent = new FieldChangeEvent();
    fieldChangeEvent.field = updatedField;
    this.dispatchEvent(fieldChangeEvent);
  }

  #test(event: Event) {
    const target = (event.target as FieldConfigPropertyElement).Element!;
    const value = target.value;
    const updatedField: Partial<FormFieldDto> = {
      id: this.field.id
    };
    updatedField.configuration = structuredClone(this.field?.configuration);
    if (Object.keys(updatedField.configuration!).includes(target.field.alias)) {
      updatedField.configuration![target.field.alias] = value?.toString() ?? "";
    }

    const fieldChangeEvent = new FieldChangeEvent();
    fieldChangeEvent.field = updatedField;
    this.dispatchEvent(fieldChangeEvent);
  }

  // A custom editor's change; plain property editors report through the dataset instead
  #onExtensionEditorChange(event: Event) {
    const target = (event.target as FieldConfigPropertyElement).Element;
    if (!target) return;
    event.stopPropagation();

    const extension = this.#getExtensionValues();
    const alias = target.field.alias.replace("extension-", "");
    if (!Object.keys(extension).includes(alias)) return;
    extension[alias] = target.value;

    const fieldChangeEvent = new FieldChangeEvent();
    fieldChangeEvent.field = { id: this.field.id, extension: extension };
    this.dispatchEvent(fieldChangeEvent);
  }

  #onConditionsChange(event: CustomEvent) {
    const conditions = event.detail;
    const updatedField: Partial<FormFieldDto> = {
      id: this.field.id,
      conditions: conditions,
    };

    const fieldChangeEvent = new FieldChangeEvent();
    fieldChangeEvent.field = updatedField;
    this.dispatchEvent(fieldChangeEvent);
  }

  protected render() {
    return html`
      <umb-property-dataset
        .value=${this._values!}
        @change=${this.#onPropertyDataChange}
      >
        <uui-tab-group class="tab-group">
          <uui-tab
            label="General"
            @click=${() => (this._activeTab = "general")}
            active=""
          ></uui-tab>
          <uui-tab
            label="Advanced"
            @click=${() => (this._activeTab = "advanced")}
          ></uui-tab>
          <uui-tab
            label="Conditions"
            @click=${() => (this._activeTab = "conditions")}
          ></uui-tab>
          ${when(
            this.#extension,
            () => html`
              <uui-tab
                label=${this.formType!.displayName}
                @click=${() => (this._activeTab = "extension")}
              ></uui-tab>
            `,
          )}
        </uui-tab-group>

        <div class="inspector-content">
          ${when(
            this._activeTab == "general",
            () => html`
              <umb-property
                alias="label"
                label="Label"
                description="Label of the field"
                property-editor-ui-alias="Umb.PropertyEditorUi.TextBox"
                .appearance=${{ labelOnTop: true }}
                val
              ></umb-property>

              <umb-property
                alias="required"
                label="Is required"
                description="Determines if the field is required"
                property-editor-ui-alias="Umb.PropertyEditorUi.Toggle"
                .appearance=${{ labelOnTop: true }}
                val
              ></umb-property>

              ${repeat(
                this.fieldType?.properties ?? [],
                (item) => item.alias,
                (item) => html`
                  <sf-field-config-property
                    .field=${{
                      ...item,
                      value: this.field!.configuration[item.alias] as string,
                    }}
                    .formField=${this.field}
                    @change=${this.#test}
                  >
                  </sf-field-config-property>
                `,
              )}
            `,
          )}
          ${when(
            this._activeTab == "advanced",
            () => html`
              <umb-property
                alias="alias"
                label="Alias"
                description="Alias of the field"
                property-editor-ui-alias="Umb.PropertyEditorUi.TextBox"
                .appearance=${{ labelOnTop: true }}
                val
              ></umb-property>
            `,
          )}
          ${when(
            this._activeTab == "extension" && this.#extension,
            () => html`
              ${repeat(
                this.#extension!.properties,
                (item) => item.alias,
                (item) => html`
                  <sf-field-config-property
                    .field=${{
                      ...item,
                      alias: "extension-" + item.alias,
                      value: this.#getExtensionValues()[item.alias],
                    }}
                    .formField=${this.field}
                    @change=${this.#onExtensionEditorChange}
                  ></sf-field-config-property>
                `,
              )}
            `,
          )}
          ${when(
            this._activeTab == "conditions",
            () => html`
              <sf-field-conditions-editor
                .field=${this.field}
                .fields=${this.fields}
                @conditions-change=${this.#onConditionsChange}
              ></sf-field-conditions-editor>
            `,
          )}
        </div>
      </umb-property-dataset>
    `;
  }

  static styles = css`
    .inspector-content {
      padding: var(--uui-size-space-5);
    }

    .tab-group {
      --uui-tab-divider: var(--uui-color-border);
      padding: 0 var(--uui-size-space-3);
      border-bottom: 1px solid var(--uui-color-border);
    }
  `;
}
