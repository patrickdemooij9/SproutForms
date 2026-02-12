import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";
import {
  css,
  customElement,
  html,
  LitElement,
  property,
  state,
  when,
} from "@umbraco-cms/backoffice/external/lit";
import { FormColumnDto, FormDefinitionDto, FormFieldDto, FormRowDto, SelectedResizeState, SelectedState } from "../models";
import SproutFormsWorkspaceContext, {
  SF_FORM_DETAIL_TOKEN_CONTEXT,
} from "./sproutFormsWorkspaceContext";

@customElement("form-canvas")
export class FormCanvas extends UmbElementMixin(LitElement) {
  private context?: SproutFormsWorkspaceContext;

  @property({ type: Object })
  selectedState!: SelectedState;

  @state()
  definition!: FormDefinitionDto;

  @state()
  private resizeState?: SelectedResizeState;

  @state()
  private draggedFieldId?: string;

  constructor() {
    super();
    this.consumeContext(SF_FORM_DETAIL_TOKEN_CONTEXT, (context) => {
      this.context = context;

      context?.form.subscribe((form) => {
        this.definition = form.definition;
      });
    });
  }

  render() {
    return html`
      <div class="canvas">
        ${this.definition.rows.map((row) => {
          const rowSize = row.columns.reduce(
            (a, b) =>
              a +
              (this.resizeState?.column == b ? this.resizeState.size : b.width),
            0
          );
          return html`
            <div class="row">
              ${row.columns.map((column) => this.renderColumn(row, column))}
              ${when(
                rowSize < 12,
                () => html`<div
                  class="column empty"
                  style="flex:${12 - rowSize}"
                  @click=${() => this.selectField(row, undefined, undefined)}
                  @dragover=${this.onDragOver}
                  @drop=${(event: DragEvent) => this.onDropOnEmpty(event, row)}
                >
                  (Empty)
                </div> `
              )}
            </div>
          `;
        })}
        <div class="row">
          <div
            class="column empty"
            style="flex:12"
            @click=${() => this.selectField(undefined, undefined, undefined)}
            @dragover=${this.onDragOver}
            @drop=${(event: DragEvent) => this.onDropOnEmpty(event, undefined)}
          >
            (Empty)
          </div>
        </div>
      </div>
    `;
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

    return html`
      <div
        class="column-outer"
        style="flex:${this.resizeState?.column == column
          ? this.resizeState.size
          : column.width}"
      >
        <div
          class="column ${this.selectedState.column == column
            ? "selected"
            : ""} ${this.draggedFieldId && this.draggedFieldId !== field.id ? 'drag-over' : ''}"
          draggable="true"
          @dragstart=${(event: DragEvent) => this.onDragStart(event, field.id!)}
          @dragover=${this.onDragOver}
          @drop=${(event: DragEvent) => this.onDrop(event, row, column)}
          @click=${() => this.selectField(row, column, field)}
        >
          ${field.label}
        </div>
        <div
          class="field-resizer"
          @mousedown=${(event: MouseEvent) =>
            this.startResize(event, row, column)}
        ></div>
      </div>
    `;
  }

  private startResize(
    event: MouseEvent,
    row: FormRowDto,
    column: FormColumnDto
  ) {
    const startX = event.clientX;
    const columnElemn = (event.target as HTMLElement)
      .previousElementSibling as HTMLElement;
    const columnElemSize = columnElemn.clientWidth;
    const singleStep = columnElemSize / column.width;

    this.resizeState = {
      column: column,
      size: column.width,
    };

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
    .canvas {
      padding: 16px;
      background: var(--uui-color-surface);
    }
    .row {
      display: flex;
      gap: 8px;
      margin-bottom: 8px;
    }
    .column-outer {
      position: relative;
    }
    .column {
      border: 1px solid #ccc;
      border-radius: 4px;
      padding: 12px;
      cursor: pointer;
      user-select: none;

      &:hover {
        background-color: #e5e7eb;
      }

      &.empty {
        border: 1px dashed #ccc;
        display: flex;
        justify-content: center;
        color: #ccc;
      }

      &.drag-over {
        border: 2px dashed #0078d4;
        background-color: #e6f2fa;
      }
    }
    .selected {
      border: 1px solid #ccc;
      background-color: #e5e7eb;
    }
    .field-resizer {
      width: 10px;
      height: 100%;
      position: absolute;
      right: -5px;
      top: 0px;
      cursor: col-resize;
    }
  `;
}
