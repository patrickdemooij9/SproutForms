import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";
import {
  css,
  customElement,
  html,
  LitElement,
  state,
} from "@umbraco-cms/backoffice/external/lit";

import "./formCanvas.element";
import "./formInspector.element";
import { FormColumnDto, FormFieldDto, FormRowDto, SelectedState } from "../models";

@customElement("form-editor")
export class FormEditorElement extends UmbElementMixin(LitElement) {
  @state()
  private selectedState: SelectedState = {
    row: null,
    field: null,
    column: null,
  };

  render() {
    return html`
      <div class="layout">
        <form-canvas
          .selectedState=${this.selectedState}
          @select-field=${this.onSelectField}
        >
        </form-canvas>

        <form-inspector
          .selectedState=${this.selectedState}
          @select-field=${this.onSelectField}
        ></form-inspector>
      </div>
    `;
  }

  private onSelectField(
    e: CustomEvent<{
      row: FormRowDto;
      column: FormColumnDto;
      field: FormFieldDto;
    }>
  ) {
    this.selectedState = {
      row: e.detail.row,
      field: e.detail.field?.id,
      column: e.detail.column,
    };
  }

  static styles = css`
    :host {
      display: block;
      height: 100%;
    }

    .layout {
      display: grid;
      grid-template-columns: minmax(0, 1fr) minmax(320px, 400px);
      height: 100%;
    }

    form-canvas {
      overflow-y: auto;
      background-color: var(--uui-color-background);
    }

    form-inspector {
      overflow-y: auto;
      border-left: 1px solid var(--uui-color-border);
      background-color: var(--uui-color-surface);
    }
  `;
}
