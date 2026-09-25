import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";
import {
  css,
  customElement,
  html,
  LitElement,
  nothing,
  property,
  state,
  when,
} from "@umbraco-cms/backoffice/external/lit";
import { FormColumnDto, FormDefinitionDto, FormFieldDto, FormFieldTypeDto, FormRowDto, SelectedResizeState, SelectedState } from "../models";
import SproutFormsWorkspaceContext, {
  SF_FORM_DETAIL_TOKEN_CONTEXT,
} from "./sproutFormsWorkspaceContext";
import { SproutFormsSource } from "../repositories/sproutFormsSource";

@customElement("form-canvas")
export class FormCanvas extends UmbElementMixin(LitElement) {
  private context?: SproutFormsWorkspaceContext;

  @property({ type: Object })
  selectedState!: SelectedState;

  @state()
  definition!: FormDefinitionDto;

  @state()
  private rows: FormRowDto[] = [];

  @state()
  private currentPageIndex = 0;

  @state()
  private draggedPageIndex?: number;

  @state()
  private fieldTypes: FormFieldTypeDto[] = [];

  @state()
  private resizeState?: SelectedResizeState;

  @state()
  private draggedFieldId?: string;

  @state()
  private isResizing = false;

  constructor() {
    super();
    this.consumeContext(SF_FORM_DETAIL_TOKEN_CONTEXT, (context) => {
      this.context = context;

      context?.form.subscribe((form) => {
        this.definition = form.definition;
      });
      this.observe(context?.currentPage, (page) => {
        this.rows = page?.rows ?? [];
      });
      this.observe(context?.currentPageIndex, (index) => {
        this.currentPageIndex = index ?? 0;
      });
    });

    new SproutFormsSource(this).getFieldTypes().then((resp) => {
      this.fieldTypes = resp.data;
    });
  }

  render() {
    const hasFields = this.rows.some((row) => row.columns.length > 0);
    const isNewRowSelected = !this.selectedState.row && !this.selectedState.column;

    return html`
      <div class="canvas">
        ${this.renderPages()}
        ${this.rows.map((row) => {
          const rowSize = row.columns.reduce(
            (a, b) =>
              a +
              (this.resizeState?.column == b ? this.resizeState.size : b.width),
            0
          );
          const isRowSlotSelected = this.selectedState.row == row && !this.selectedState.column;
          return html`
            <div class="row">
              ${row.columns.map((column) => this.renderColumn(row, column))}
              ${when(
                rowSize < 12,
                () => html`<button
                  class="drop-zone ${isRowSlotSelected ? "selected" : ""}"
                  style="flex:${12 - rowSize}"
                  title="Add a field to this row"
                  @click=${() => this.selectField(row, undefined, undefined)}
                  @dragover=${this.onDragOver}
                  @drop=${(event: DragEvent) => this.onDropOnEmpty(event, row)}
                >
                  <uui-icon name="icon-add"></uui-icon>
                </button>`
              )}
            </div>
          `;
        })}
        <button
          class="drop-zone new-row ${isNewRowSelected ? "selected" : ""}"
          @click=${() => this.selectField(undefined, undefined, undefined)}
          @dragover=${this.onDragOver}
          @drop=${(event: DragEvent) => this.onDropOnEmpty(event, undefined)}
        >
          <uui-icon name="icon-add"></uui-icon>
          ${hasFields
            ? "Add a row"
            : "Pick a field from the panel to start building your form"}
        </button>
      </div>
    `;
  }

  private renderPages() {
    return html`
      <div class="pages" role="tablist" aria-label="Pages">
        ${this.definition.pages.map((page, index) => {
          const isCurrent = index === this.currentPageIndex;
          const hasConditions = (page.visibility?.rules?.length ?? 0) > 0;
          return html`
            <button
              class="page-tab ${isCurrent ? "current" : ""} ${this.draggedPageIndex === index ? "dragging" : ""}"
              role="tab"
              aria-selected=${isCurrent}
              title=${isCurrent ? "Page settings" : "Show this page. Drop a field here to move it to this page"}
              draggable="true"
              @click=${() => this.selectPage(index)}
              @dragstart=${(event: DragEvent) => this.onPageDragStart(event, index)}
              @dragend=${this.onDragEnd}
              @dragover=${this.onDragOver}
              @drop=${(event: DragEvent) => this.onDropOnPage(event, index)}
            >
              <span class="page-number">${index + 1}</span>
              <span class="page-title">${page.title || `Page ${index + 1}`}</span>
              ${hasConditions
                ? html`<uui-icon name="icon-directions" title="Only shown when its conditions hold"></uui-icon>`
                : nothing}
            </button>
          `;
        })}
        <button class="page-add" title="Add a page at the end" @click=${this.addPage}>
          <uui-icon name="icon-add"></uui-icon> Add page
        </button>
      </div>
    `;
  }

  private selectPage(index: number) {
    this.context?.setCurrentPage(index);
    this.dispatchEvent(new CustomEvent("select-page", { bubbles: true, composed: true }));
  }

  private addPage() {
    this.context?.addPage();
    this.dispatchEvent(new CustomEvent("select-page", { bubbles: true, composed: true }));
  }

  private onPageDragStart(event: DragEvent, index: number) {
    this.draggedPageIndex = index;
    if (event.dataTransfer) {
      event.dataTransfer.effectAllowed = "move";
      event.dataTransfer.setData("text/plain", `page-${index}`);
    }
  }

  // A field dropped on a tab moves to that page; a tab dropped on a tab takes its place
  private onDropOnPage(event: DragEvent, index: number) {
    event.preventDefault();
    if (this.draggedFieldId) {
      this.context?.moveFieldToPage(this.draggedFieldId, index);
      this.draggedFieldId = undefined;
      this.selectPage(index);
    } else if (this.draggedPageIndex !== undefined) {
      this.context?.movePage(this.draggedPageIndex, index);
      this.draggedPageIndex = undefined;
    }
  }

  private renderColumn(
    row: FormRowDto,
    column: FormColumnDto
  ) {
    const field = this.definition.fields.find(
      (f) => f.id === column.fieldId
    );
    if (!field) {
      return;
    }

    const fieldType = this.fieldTypes.find((it) => it.alias === field.fieldTypeAlias);
    const isResizingThis = this.resizeState?.column == column;
    const width = isResizingThis ? this.resizeState!.size : column.width;

    return html`
      <div class="column-outer" style="flex:${width}">
        <div
          class="field ${this.selectedState.column == column
            ? "selected"
            : ""} ${this.draggedFieldId && this.draggedFieldId !== field.id ? 'drag-over' : ''} ${this.draggedFieldId === field.id ? 'dragging' : ''}"
          draggable="${this.isResizing ? 'false' : 'true'}"
          @dragstart=${(event: DragEvent) => this.onDragStart(event, field.id!)}
          @dragend=${this.onDragEnd}
          @dragover=${this.onDragOver}
          @drop=${(event: DragEvent) => this.onDrop(event, row, column)}
          @click=${() => this.selectField(row, column, field)}
        >
          <uui-icon class="grip" name="icon-grip"></uui-icon>
          <span class="field-icon">
            <umb-icon name=${fieldType?.icon ?? "icon-document"}></umb-icon>
          </span>
          <span class="field-text">
            <span class="field-label">
              ${field.label}
              ${field.required ? html`<span class="required" title="Required">*</span>` : nothing}
            </span>
            <span class="field-type">${fieldType?.displayName ?? field.fieldTypeAlias}</span>
          </span>
          ${when(
            // A condition whose rules were all removed is still stored, so look for rules
            (field.conditions?.visibility?.rules?.length ?? 0) + (field.conditions?.required?.rules?.length ?? 0) > 0,
            () => html`<uui-icon class="badge-icon" name="icon-directions" title="Has conditions"></uui-icon>`,
          )}
          ${when(
            isResizingThis,
            () => html`<span class="width-badge">${width}/12</span>`,
          )}
          <button
            class="delete-btn"
            @click=${(event: MouseEvent) => this.deleteField(event, field.id!)}
            title="Delete field"
            aria-label="Delete field"
          >
            <uui-icon name="icon-trash"></uui-icon>
          </button>
        </div>
        <div
          class="field-resizer ${isResizingThis ? "active" : ""}"
          draggable="false"
          title="Drag to resize"
          @mousedown=${(event: MouseEvent) =>
            this.startResize(event, row, column)}
        ></div>
      </div>
    `;
  }

  private deleteField(event: MouseEvent, fieldId: string) {
    event.stopPropagation();
    this.context?.removeField(fieldId);
  }

  private startResize(
    event: MouseEvent,
    row: FormRowDto,
    column: FormColumnDto
  ) {
    event.preventDefault();
    event.stopPropagation();

    const startX = event.clientX;
    const columnElemn = (event.target as HTMLElement)
      .previousElementSibling as HTMLElement;
    const columnElemSize = columnElemn.clientWidth;
    const singleStep = columnElemSize / column.width;

    this.resizeState = {
      column: column,
      size: column.width,
    };

    this.isResizing = true;

    const onMouseMove = (moveEvent: MouseEvent) => {
      const newColumnElemSize = columnElemSize - (startX - moveEvent.clientX);

      let newWidth = Math.round(newColumnElemSize / singleStep);
      if (newWidth > 12) newWidth = 12;
      if (newWidth < 1) newWidth = 1;
      if (column.width != newWidth) {
        this.resizeState!.size = newWidth;
        this.requestUpdate();
      }
    };

    const onMouseUp = () => {
      document.removeEventListener("mousemove", onMouseMove);
      document.removeEventListener("mouseup", onMouseUp);

      this.context?.setColumnSize(row, column, this.resizeState!.size);
      this.resizeState = undefined;
      this.isResizing = false;
    };

    document.addEventListener("mousemove", onMouseMove);
    document.addEventListener("mouseup", onMouseUp);
  }

  private selectField(
    row: FormRowDto | undefined,
    column: FormColumnDto | undefined,
    field: FormFieldDto | undefined
  ) {
    this.dispatchEvent(
      new CustomEvent("select-field", {
        detail: { row, column, field },
        bubbles: true,
        composed: true,
      })
    );
  }

  private onDragStart(event: DragEvent, fieldId: string) {
    this.draggedFieldId = fieldId;
    if (event.dataTransfer) {
      event.dataTransfer.effectAllowed = 'move';
      event.dataTransfer.setData('text/plain', fieldId);
    }
  }

  // A drag that is cancelled never reaches a drop target
  private onDragEnd() {
    this.draggedFieldId = undefined;
    this.draggedPageIndex = undefined;
  }

  private onDragOver(event: DragEvent) {
    event.preventDefault();
    if (event.dataTransfer) {
      event.dataTransfer.dropEffect = 'move';
    }
  }

  private onDrop(event: DragEvent, targetRow: FormRowDto, targetColumn: FormColumnDto) {
    event.preventDefault();
    if (!this.draggedFieldId) return;

    this.context?.moveField(this.draggedFieldId, targetRow, targetColumn);
    this.draggedFieldId = undefined;
  }

  private onDropOnEmpty(event: DragEvent, targetRow: FormRowDto | undefined) {
    event.preventDefault();
    if (!this.draggedFieldId) return;

    this.context?.moveField(this.draggedFieldId, targetRow);
    this.draggedFieldId = undefined;
  }

  static styles = css`
    :host {
      display: block;
    }

    .canvas {
      max-width: 960px;
      margin: 0 auto;
      padding: var(--uui-size-layout-1);
      display: flex;
      flex-direction: column;
      gap: var(--uui-size-space-3);
    }

    .row {
      display: flex;
      gap: var(--uui-size-space-3);
    }

    .pages {
      display: flex;
      flex-wrap: wrap;
      gap: var(--uui-size-space-2);
      margin-bottom: var(--uui-size-space-3);
    }

    .page-tab,
    .page-add {
      display: flex;
      align-items: center;
      gap: var(--uui-size-space-2);
      min-width: 0;
      padding: var(--uui-size-space-2) var(--uui-size-space-4);
      font: inherit;
      color: var(--uui-color-text);
      background: var(--uui-color-surface);
      border: 1px solid var(--uui-color-border);
      border-radius: calc(var(--uui-border-radius) * 2);
      cursor: pointer;
      transition: border-color 120ms, box-shadow 120ms;

      &:hover {
        border-color: var(--uui-color-border-emphasis);
      }
    }

    .page-tab {
      max-width: 240px;

      &.current {
        border-color: var(--uui-color-selected);
        box-shadow: 0 0 0 1px var(--uui-color-selected);
        font-weight: 700;
      }

      &.dragging {
        opacity: 0.5;
      }
    }

    .page-number {
      display: flex;
      align-items: center;
      justify-content: center;
      flex-shrink: 0;
      width: 20px;
      height: 20px;
      border-radius: 50%;
      background: var(--uui-color-surface-alt);
      font-size: var(--uui-type-small-size);
    }

    .page-tab.current .page-number {
      background: var(--uui-color-selected);
      color: var(--uui-color-selected-contrast);
    }

    .page-title {
      overflow: hidden;
      text-overflow: ellipsis;
      white-space: nowrap;
    }

    .page-add {
      color: var(--uui-color-interactive);
      border-style: dashed;
    }

    .column-outer {
      position: relative;
      min-width: 0;
    }

    .field {
      display: flex;
      align-items: center;
      gap: var(--uui-size-space-3);
      min-height: 56px;
      box-sizing: border-box;
      padding: var(--uui-size-space-3) var(--uui-size-space-4);
      padding-left: var(--uui-size-space-2);
      background: var(--uui-color-surface);
      border: 1px solid var(--uui-color-border);
      border-radius: calc(var(--uui-border-radius) * 2);
      box-shadow: var(--uui-shadow-depth-1);
      cursor: pointer;
      user-select: none;
      transition: border-color 120ms, box-shadow 120ms;

      &:hover {
        border-color: var(--uui-color-border-emphasis);
      }

      &.selected {
        border-color: var(--uui-color-selected);
        box-shadow: 0 0 0 1px var(--uui-color-selected);
      }

      &.dragging {
        opacity: 0.5;
      }

      &.drag-over {
        border-style: dashed;
        border-color: var(--uui-color-interactive-emphasis);
      }
    }

    .grip {
      color: var(--uui-color-disabled-contrast);
      cursor: grab;
      flex-shrink: 0;
    }

    .field-icon {
      display: flex;
      align-items: center;
      justify-content: center;
      flex-shrink: 0;
      width: 32px;
      height: 32px;
      border-radius: var(--uui-border-radius);
      background: var(--uui-color-surface-alt);
      color: var(--uui-color-interactive);
    }

    .field-text {
      display: flex;
      flex-direction: column;
      flex: 1;
      min-width: 0;
    }

    .field-label,
    .field-type {
      overflow: hidden;
      text-overflow: ellipsis;
      white-space: nowrap;
    }

    .field-label {
      font-weight: 700;
    }

    .required {
      color: var(--uui-color-danger);
    }

    .field-type {
      font-size: var(--uui-type-small-size);
      color: var(--uui-color-text-alt);
    }

    .badge-icon {
      flex-shrink: 0;
      color: var(--uui-color-text-alt);
    }

    .width-badge {
      flex-shrink: 0;
      padding: 0 var(--uui-size-space-2);
      border-radius: var(--uui-border-radius);
      background: var(--uui-color-selected);
      color: var(--uui-color-selected-contrast);
      font-size: var(--uui-type-small-size);
      font-weight: 700;
    }

    .delete-btn {
      display: flex;
      align-items: center;
      justify-content: center;
      flex-shrink: 0;
      padding: var(--uui-size-space-2);
      background: none;
      border: none;
      border-radius: var(--uui-border-radius);
      color: var(--uui-color-text-alt);
      cursor: pointer;
      opacity: 0;
      transition: opacity 120ms, color 120ms, background-color 120ms;

      &:hover {
        color: var(--uui-color-danger);
        background-color: var(--uui-color-surface-emphasis);
      }

      &:focus-visible {
        opacity: 1;
      }
    }

    .field:hover .delete-btn,
    .field.selected .delete-btn {
      opacity: 1;
    }

    .drop-zone {
      display: flex;
      align-items: center;
      justify-content: center;
      gap: var(--uui-size-space-2);
      min-width: 0;
      min-height: 56px;
      box-sizing: border-box;
      padding: var(--uui-size-space-3);
      font: inherit;
      color: var(--uui-color-text-alt);
      background: transparent;
      border: 1px dashed var(--uui-color-border-emphasis);
      border-radius: calc(var(--uui-border-radius) * 2);
      cursor: pointer;
      transition: border-color 120ms, color 120ms, background-color 120ms;

      &:hover,
      &.selected {
        color: var(--uui-color-interactive-emphasis);
        border-color: var(--uui-color-interactive-emphasis);
        background-color: var(--uui-color-surface);
      }

      &.selected {
        border-style: solid;
      }

      &.new-row {
        width: 100%;
      }
    }

    .field-resizer {
      position: absolute;
      top: 50%;
      right: calc(var(--uui-size-space-3) / -2 - 3px);
      width: 6px;
      height: 24px;
      transform: translateY(-50%);
      border-radius: 3px;
      background: var(--uui-color-border-emphasis);
      cursor: col-resize;
      opacity: 0;
      transition: opacity 120ms;

      &::before {
        content: "";
        position: absolute;
        inset: -16px -6px;
      }

      &.active {
        opacity: 1;
        background: var(--uui-color-selected);
      }
    }

    .column-outer:hover .field-resizer {
      opacity: 1;
    }
  `;
}
