import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";
import {
  css,
  customElement,
  html,
  LitElement,
  nothing,
  property,
  PropertyValues,
  state,
} from "@umbraco-cms/backoffice/external/lit";

import "./formFieldSelector.element";
import { FormDefinitionDto, FormDefinitionTypeDto, FormFieldDto, FormFieldTypeDto, SelectedState } from "../models";
import {
  UmbPropertyValueData,
} from "@umbraco-cms/backoffice/property";
import SproutFormsWorkspaceContext, {
  SF_FORM_DETAIL_TOKEN_CONTEXT,
} from "./sproutFormsWorkspaceContext";
import { SproutFormsSource } from "../repositories/sproutFormsSource";
import { FieldChangeEvent } from "./formInspectorFieldType.element";

import "./formInspectorFieldType.element";
import "./formPageSettings.element";

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

  @state()
  private formType?: FormDefinitionTypeDto;

  constructor() {
    super();
    this.consumeContext(SF_FORM_DETAIL_TOKEN_CONTEXT, (context) => {
      this.context = context;

      this.observe(context?.formType, (formType) => {
        this.formType = formType;
      });

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

  // A field's conditions can use the fields on its own page and earlier pages
  #getConditionFields(field: FormFieldDto) {
    const pageIndex = this.context?.getPageIndexOfField(field.id) ?? -1;
    return pageIndex === -1 ? [] : this.context!.getFieldsBeforePage(pageIndex, true);
  }

  #renderMoveToPage(field: FormFieldDto) {
    if (this.definition.pages.length < 2) return nothing;

    const pageIndex = this.context?.getPageIndexOfField(field.id) ?? -1;
    return html`
      <div class="move-to-page">
        <uui-label for="move-to-page">Page</uui-label>
        <select
          id="move-to-page"
          .value=${String(pageIndex)}
          @change=${(e: Event) => this.#moveToPage(field, Number((e.target as HTMLSelectElement).value))}
        >
          ${this.definition.pages.map(
            (page, index) => html`<option value=${index} ?selected=${index === pageIndex}>
              ${page.title || `Page ${index + 1}`}
            </option>`,
          )}
        </select>
      </div>
    `;
  }

  // Follows the field to its new page, so it stays selected
  #moveToPage(field: FormFieldDto, pageIndex: number) {
    this.context?.moveFieldToPage(field.id, pageIndex);
    this.context?.setCurrentPage(pageIndex);
    const row = this.context?.getCurrentPageRows().find((it) => it.columns.some((col) => col.fieldId === field.id));
    this.dispatchEvent(
      new CustomEvent("select-field", {
        detail: {
          row,
          column: row?.columns.find((col) => col.fieldId === field.id),
          field,
        },
        bubbles: true,
        composed: true,
      }),
    );
  }

  #handleFieldUpdate(event: FieldChangeEvent) {
    this.context?.updateField(event.field);
  }

  getFieldType(fieldTypeAlias: string) {
    return this.fieldTypes.find((item) => item.alias === fieldTypeAlias);
  }

  render() {
    const fieldType = this.selectedField
      ? this.getFieldType(this.selectedField.fieldTypeAlias)
      : undefined;

    return html`
      <div class="inspector">
        ${
          this.selectedState.field && this.selectedField
            ? html`
                <div class="panel-header">
                  <span class="panel-icon">
                    <umb-icon name=${fieldType?.icon ?? "icon-document"}></umb-icon>
                  </span>
                  <div class="panel-title">
                    <h3>${this.selectedField.label}</h3>
                    <span>${fieldType?.displayName ?? this.selectedField.fieldTypeAlias}</span>
                  </div>
                  <uui-button
                    compact
                    label="Close"
                    title="Back to the field list"
                    @click=${this.#deselect}
                  >
                    <uui-icon name="icon-wrong"></uui-icon>
                  </uui-button>
                </div>
                ${this.#renderMoveToPage(this.selectedField)}
                <sf-inspector-field-type
                  .field=${this.selectedField}
                  .fieldType=${fieldType!}
                  .fields=${this.#getConditionFields(this.selectedField)}
                  .formType=${this.formType}
                  @field-change=${this.#handleFieldUpdate}>
                </sf-inspector-field-type>
              `
            : this.selectedState.pageSettings
            ? html`<sf-page-settings></sf-page-settings>`
            : html`
                <div class="panel-header">
                  <div class="panel-title">
                    <h3>Add a field</h3>
                    <span>${this.selectedState.row
                      ? "It is added to the selected row"
                      : "It is added as a new row"}</span>
                  </div>
                </div>
                <div class="inspector-content">
                  <form-field-selector
                    @add-field=${(e: any) => this.onAddField(e.detail)}
                  ></form-field-selector>
                </div>
              `
        }
      </div>
    `;
  }

  #deselect() {
    this.dispatchEvent(
      new CustomEvent("select-field", {
        detail: { row: undefined, column: undefined, field: undefined },
        bubbles: true,
        composed: true,
      }),
    );
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
    const extension = this.formType?.fieldExtensions.find(
      (it) => it.fieldTypeAlias === fieldType.alias,
    );
    if (extension) {
      newField.extension = {};
      extension.properties.forEach((prop) => {
        newField.extension![prop.alias] = prop.value ?? null;
      });
    }
    // The rows in the workspace state are frozen, so place the field in a copy
    const rows = structuredClone(this.context?.getCurrentPageRows() ?? []);
    let row = rows.find((it) => it.id === this.selectedState.row?.id);
    let column = row?.columns.find((it) => it.id === this.selectedState.column?.id);
    if (column) {
      column.fieldId = newField.id;
    } else {
      if (!row) {
        row = { id: crypto.randomUUID(), columns: [] };
        rows.push(row);
      }
      const rowSize = row.columns.reduce((a, b) => a + b.width, 0);
      column = {
        id: crypto.randomUUID(),
        width: 12 - rowSize,
        fieldId: newField.id,
      };
      row.columns.push(column);
    }
    newDefinition.fields = [...newDefinition.fields, newField];
    this.context?.updateForm({
      definition: this.context.withCurrentPageRows(newDefinition, rows),
    });

    // Select the new field, using the row and column objects the canvas now renders
    const newRow = this.context?.getCurrentPageRows().find((it) => it.id === row!.id);
    this.dispatchEvent(
      new CustomEvent("select-field", {
        detail: {
          row: newRow,
          column: newRow?.columns.find((it) => it.id === column!.id),
          field: newField,
        },
        bubbles: true,
        composed: true,
      }),
    );
  }

  static styles = css`
    .move-to-page {
      display: flex;
      align-items: center;
      gap: var(--uui-size-space-3);
      padding: var(--uui-size-space-3) var(--uui-size-space-5);
      border-bottom: 1px solid var(--uui-color-border);
    }

    .move-to-page select {
      flex: 1;
      padding: 6px 10px;
      border: 1px solid var(--uui-color-border);
      border-radius: var(--uui-border-radius);
      background-color: var(--uui-color-surface);
      color: var(--uui-color-text);
      font: inherit;
    }

    .panel-header {
      display: flex;
      align-items: center;
      gap: var(--uui-size-space-3);
      padding: var(--uui-size-space-4) var(--uui-size-space-5);
      border-bottom: 1px solid var(--uui-color-border);
    }

    .panel-icon {
      display: flex;
      align-items: center;
      justify-content: center;
      flex-shrink: 0;
      width: 36px;
      height: 36px;
      border-radius: var(--uui-border-radius);
      background: var(--uui-color-surface-alt);
      color: var(--uui-color-interactive);
      font-size: 1.1em;
    }

    .panel-title {
      display: flex;
      flex-direction: column;
      flex: 1;
      min-width: 0;

      h3 {
        margin: 0;
        overflow: hidden;
        text-overflow: ellipsis;
        white-space: nowrap;
      }

      span {
        font-size: var(--uui-type-small-size);
        color: var(--uui-color-text-alt);
      }
    }

    .inspector-content {
      padding: var(--uui-size-space-5);
    }
  `;
}
