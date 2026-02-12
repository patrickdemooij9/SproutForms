import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";
import {
  css,
  customElement,
  html,
  LitElement,
  property,
  PropertyValues,
  state,
} from "@umbraco-cms/backoffice/external/lit";

import "./formFieldSelector.element";
import { FormDefinitionDto, FormFieldDto, FormFieldTypeDto, SelectedState } from "../models";
import {
  UmbPropertyValueData,
} from "@umbraco-cms/backoffice/property";
import SproutFormsWorkspaceContext, {
  SF_FORM_DETAIL_TOKEN_CONTEXT,
} from "./sproutFormsWorkspaceContext";
import { SproutFormsSource } from "../repositories/sproutFormsSource";
import { FieldChangeEvent } from "./formInspectorFieldType.element";

import "./formInspectorFieldType.element";

@customElement("form-inspector")
export class FormInspector extends UmbElementMixin(LitElement) {
  private context?: SproutFormsWorkspaceContext;

  @property({ type: Object })
  selectedState!: SelectedState;

  @state()
  definition!: FormDefinitionDto;

  @state()
  selectedField?: FormFieldDto;

  @state()
  _values: Array<UmbPropertyValueData> = [];

  @state()
  private fieldTypes: FormFieldTypeDto[] = [];

  constructor() {
    super();
    this.consumeContext(SF_FORM_DETAIL_TOKEN_CONTEXT, (context) => {
      this.context = context;

      context?.form.subscribe((form) => {
        this.definition = form.definition;
        this.selectedField = form.definition.fields.find(
          (item) => item.id === this.selectedState.field,
        );
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
        (item) => item.id === selectedState.field,
      );
    }
  }

  #handleFieldUpdate(event: FieldChangeEvent) {
    this.context?.updateField(event.field);
  }

  getFieldType(fieldTypeAlias: string) {
    return this.fieldTypes.find((item) => item.alias === fieldTypeAlias);
  }

  render() {
    return html`
      <div class="inspector">
        ${
          this.selectedState.field
            ? html`
                <div class="fields">
                  <sf-inspector-field-type
                    .field=${this.selectedField!}
                    .fieldType=${this.getFieldType(this.selectedField!.fieldTypeAlias)!}
                    @field-change=${this.#handleFieldUpdate}>

                  </sf-inspector-field-type>
                </div>
              `
            : html`<div class="inspector-content">
                <h3>Fields</h3>
                <form-field-selector
                  @add-field=${(e: any) => this.onAddField(e.detail)}
                ></form-field-selector>
              </div>`
        }
          </div>
      </div>
    `;
  }

  private onAddField(fieldType: FormFieldTypeDto) {
    const newDefinition = { ...this.definition };
    const configuration: Record<string, any> = {};
    fieldType.properties.forEach((prop) => {
      configuration[prop.alias] = prop.value;
    });
    const newField: FormFieldDto = {
      id: crypto.randomUUID(),
      label: fieldType.displayName,
      alias: crypto.randomUUID(),
      fieldTypeAlias: fieldType.alias,
      required: false,
      configuration: configuration,
    };
    if (this.selectedState.column) {
      this.selectedState.column!.fieldId = newField.id;
    } else {
      let row = this.selectedState.row;
      if (!row) {
        row = { id: crypto.randomUUID(), columns: [] };
        newDefinition.rows = [...newDefinition.rows, row];
      }
      const rowSize = row.columns.reduce((a, b) => a + b.width, 0);
      const newColumn = {
        id: crypto.randomUUID(),
        width: 12 - rowSize,
        fieldId: newField.id,
      };
      row.columns.push(newColumn);
      this.selectedState.column = newColumn;
    }
    this.selectedState.field = newField.id;
    newDefinition.fields = [...newDefinition.fields, newField];
    this.context?.updateForm({ definition: newDefinition });
  }

  static styles = css`
    .fields {
      display: flex;
      gap: 8px;
      flex-direction: column;
    }

    .inspector-content {
      padding: 18px 12px;
    }
  `;
}
