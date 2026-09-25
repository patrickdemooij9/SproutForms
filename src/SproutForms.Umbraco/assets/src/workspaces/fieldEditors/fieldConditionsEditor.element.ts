import { css, html, LitElement } from "lit";
import { property, state } from "lit/decorators.js";
import { customElement } from "@umbraco-cms/backoffice/external/lit";
import { FormFieldDto } from "../../models";
import { FieldConditions } from "../../api";
import { ConditionChangeEvent } from "./conditionEditor.element";
import "./conditionEditor.element";

@customElement("sf-field-conditions-editor")
export class FieldConditionsEditor extends LitElement {
  @property({ type: Object })
  public field!: FormFieldDto;

  // The fields its conditions can use: those on the field's own page and earlier pages
  @property({ type: Array })
  public fields: FormFieldDto[] = [];

  @state()
  private _conditions: FieldConditions = {};

  connectedCallback() {
    super.connectedCallback();
    this._conditions = this.field.conditions ?? {};
  }

  #getAvailableFields() {
    return this.fields.filter((f) => f.id !== this.field.id);
  }

  #onConditionChange(conditionType: "visibility" | "required", event: ConditionChangeEvent) {
    // Handled here, so the parent only hears about the field's conditions as a whole
    event.stopPropagation();
    this._conditions = {
      ...this._conditions,
      [conditionType]: event.condition,
    };

    this.dispatchEvent(
      new CustomEvent("conditions-change", {
        detail: this._conditions,
        bubbles: true,
        composed: true,
      }),
    );
  }

  render() {
    const availableFields = this.#getAvailableFields();

    if (availableFields.length === 0) {
      return html`<p class="no-fields">
        No other fields on this page or earlier pages to create conditions with.
      </p>`;
    }

    return html`
      <div class="conditions-editor">
        <sf-condition-editor
          label="Show this field when"
          .condition=${this._conditions.visibility}
          .fields=${availableFields}
          @condition-change=${(e: ConditionChangeEvent) => this.#onConditionChange("visibility", e)}
        ></sf-condition-editor>
        <sf-condition-editor
          label="Make required when"
          .condition=${this._conditions.required}
          .fields=${availableFields}
          @condition-change=${(e: ConditionChangeEvent) => this.#onConditionChange("required", e)}
        ></sf-condition-editor>
      </div>
    `;
  }

  static styles = css`
    .conditions-editor {
      display: flex;
      flex-direction: column;
      gap: 16px;
    }

    .no-fields {
      color: var(--uui-color-text-alt);
      font-style: italic;
    }
  `;
}
