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

        <form-inspector .selectedState=${this.selectedState}> </form-inspector>
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
    .layout {
      display: grid;
      grid-template-columns: 2fr 1fr;
      height: 100%;

      position: relative;
    }

    form-inspector,
    form-canvas {
      overflow-y: auto;
      background-color: white;
    }

    form-inspector {
      border-left: 1px solid #ccc;
    }

    .overlay {
      position: absolute;
      width: 100%;
      height: 100%;
      background-color: rgba(0, 0, 0, 0.3);

      display: flex;
      justify-content: center;
      align-items: center;

      p {
        padding: 16px 24px;
        background-color: white;
        border-radius: 4px;
      }
    }
  `;
}
