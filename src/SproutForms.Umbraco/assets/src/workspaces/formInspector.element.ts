import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";
import {
  css,
  customElement,
  html,
  LitElement,
  property,
  PropertyValues,
  repeat,
  state,
} from "@umbraco-cms/backoffice/external/lit";

import "./formFieldSelector.element";
import "./fieldEditors/fieldConfigProperty.element";
import {
  FormDefinitionBackofficeModel,
  FormFieldBackofficeModel,
  FormFieldTypeBackofficeModel,
} from "../api";
import { SelectedState } from "../models";
import {
  UmbPropertyDatasetElement,
  UmbPropertyValueData,
} from "@umbraco-cms/backoffice/property";
import SproutFormsWorkspaceContext, {
  SF_FORM_DETAIL_TOKEN_CONTEXT,
} from "./sproutFormsWorkspaceContext";
import { FieldConfigPropertyElement } from "./fieldEditors/fieldConfigProperty.element";
import { SproutFormsSource } from "../repositories/sproutFormsSource";

@customElement("form-inspector")
export class FormInspector extends UmbElementMixin(LitElement) {
  private context?: SproutFormsWorkspaceContext;

  @property({ type: Object })
  selectedState!: SelectedState;

  @state()
  definition!: FormDefinitionBackofficeModel;

  @state()
  selectedField?: FormFieldBackofficeModel;

  @state()
  _values: Array<UmbPropertyValueData> = [];

  @state()
  private fieldTypes: FormFieldTypeBackofficeModel[] = [];

  constructor() {
    super();
    this.consumeContext(SF_FORM_DETAIL_TOKEN_CONTEXT, (context) => {
      this.context = context;

      context?.form.subscribe((form) => {
        this.definition = form.definition;
        this.selectedField = form.definition.fields.find(
          (item) => item.alias === this.selectedState.field
        );
        this.setValues();
      });
    });

    //TODO: Move to context?
    new SproutFormsSource(this).getFieldTypes().then((resp) => {
      this.fieldTypes = resp.data;
    });
  }

  protected willUpdate(_changedProperties: PropertyValues): void {
    if (_changedProperties.has("selectedState")) {
      const selectedState = this.selectedState;
      if (!selectedState || !selectedState.field) {
        return;
      }
      this.selectedField = this.definition.fields.find(
        (item) => item.alias === selectedState.field
      );
      this.setValues();
    }
  }

  private setValues() {
    if (!this.selectedField) {
      this._values = [];
      return;
    }
    this._values = [
      {
        alias: "label",
        value: this.selectedField!.label,
      },
      {
        alias: "required",
        value: this.selectedField!.required,
      },
    ];
    Object.entries(this.selectedField!.configuration).forEach(
      ([key, value]) => {
        this._values.push({
          alias: key,
          value: value,
        });
      }
    );
  }

  #onPropertyDataChange(e: Event) {
    if (!this.selectedState.field) {
      return;
    }

    const value = (e.target as UmbPropertyDatasetElement).value;

    const updatedField: Partial<FormFieldBackofficeModel> = {};
    updatedField.configuration = structuredClone(
      this.selectedField?.configuration
    );
    console.log("Update!");
    value.forEach((item) => {
      if (item.alias == "label") {
        updatedField.label = item.value as string;
      } else if (item.alias == "required") {
        updatedField.required = item.value as boolean;
      } else {
        if (Object.keys(updatedField.configuration!).includes(item.alias)) {
          updatedField.configuration![item.alias] =
            item.value?.toString() ?? "";
        }
      }
    });
    this.context?.updateField({
      ...updatedField,
      alias: this.selectedState.field,
    });
  }

  #test(event: Event) {
    if (!this.selectedState.field) {
      return;
    }

    const target = (event.target as FieldConfigPropertyElement).Element!;
    const value = target.value;
    const updatedField: Partial<FormFieldBackofficeModel> = {};
    updatedField.configuration = structuredClone(
      this.selectedField?.configuration
    );
    if (Object.keys(updatedField.configuration!).includes(target.field.alias)) {
      updatedField.configuration![target.field.alias] = value?.toString() ?? "";
    }

    this.context?.updateField({
      ...updatedField,
      alias: this.selectedState.field,
    });
  }

  getFieldType(fieldTypeId: string) {
    return this.fieldTypes.find((item) => item.id === fieldTypeId);
  }

  render() {
    return html`
      <div class="inspector">
        ${this.selectedState.field
          ? html`
              <h3>Field settings</h3>
              <div class="fields">
                <umb-property-dataset
                  .value=${this._values!}
                  @change=${this.#onPropertyDataChange}
                >
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
                    this.getFieldType(this.selectedField!.fieldTypeId)
                      ?.properties ?? [],
                    (item) => item.alias,
                    (item) => html`
                      <sf-field-config-property
                        .field=${{
                          ...item,
                          value: this.selectedField!.configuration[item.alias],
                        }}
                        @change=${this.#test}
                      >
                      </sf-field-config-property>
                    `
                  )}
                </umb-property-dataset>
              </div>
            `
          : html` <h3>Fields</h3>
              <form-field-selector
                @add-field=${(e: any) => this.onAddField(e.detail)}
              ></form-field-selector>`}
      </div>
    `;
  }

  private onAddField(fieldType: FormFieldTypeBackofficeModel) {
    const newDefinition = { ...this.definition };
    const configuration: Record<string, any> = {};
    fieldType.properties.forEach((prop) => {
      configuration[prop.alias] = prop.value;
    });
    const newField: FormFieldBackofficeModel = {
      label: fieldType.displayName,
      alias: crypto.randomUUID(),
      fieldTypeId: fieldType.id,
      required: false,
      configuration: configuration,
    };
    if (this.selectedState.column) {
      this.selectedState.column!.fieldAlias = newField.alias;
    } else {
      let row = this.selectedState.row;
      if (!row) {
        row = { columns: [] };
        newDefinition.rows = [...newDefinition.rows, row];
      }
      const rowSize = row.columns.reduce((a, b) => a + b.width, 0);
      const newColumn = {
        width: 12 - rowSize,
        fieldAlias: newField.alias,
      };
      row.columns.push(newColumn);
      this.selectedState.column = newColumn;
    }
    this.selectedState.field = newField.alias;
    newDefinition.fields = [...newDefinition.fields, newField];
    this.context?.updateForm({ definition: newDefinition });
  }

  static styles = css`
    .inspector {
      padding: 18px 12px;
    }

    .fields {
      display: flex;
      gap: 8px;
      flex-direction: column;
    }
  `;
}
