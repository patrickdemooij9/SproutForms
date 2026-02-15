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
import { FormFieldDto, FormFieldTypeDto, FormDefinitionDto } from "../models";

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
  }

  #onPropertyDataChange(e: Event) {
    const value = (e.target as UmbPropertyDatasetElement).value;

    const updatedField: Partial<FormFieldDto> = {
      id: this.field.id
    };
    updatedField.configuration = structuredClone(this.field?.configuration);
    value.forEach((item) => {
      if (item.alias == "label") {
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
                val
              ></umb-property>

              <umb-property
                alias="required"
                label="Is required"
                description="Determines if the field is required"
                property-editor-ui-alias="Umb.PropertyEditorUi.Toggle"
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
                val
              ></umb-property>
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
      padding: 18px 12px;
    }

    .tab-group {
      border-bottom: 1px solid #ccc;
    }
  `;
}
