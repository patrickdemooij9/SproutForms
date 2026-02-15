import { css, html, LitElement } from "lit";
import { property, state } from "lit/decorators.js";
import { customElement } from "@umbraco-cms/backoffice/external/lit";
import { FormDefinitionDto, FormFieldDto } from "../../models";
import { ConditionComparison, ConditionDefinition, ConditionRule, FieldConditions } from "../../api";
import "@umbraco-cms/backoffice/external/uui";

@customElement("sf-field-conditions-editor")
export class FieldConditionsEditor extends LitElement {
  @property({ type: Object })
  public field!: FormFieldDto;

  @property({ type: Array })
  public fields!: FormDefinitionDto["fields"];

  @state()
  private _conditions: FieldConditions = {};

  connectedCallback() {
    super.connectedCallback();
    this._conditions = this.field.conditions ?? {};
  }

  #getAvailableFields() {
    return this.fields.filter((f) => f.id !== this.field.id);
  }

  #getConditionRules(
    conditionType: "visibility" | "required",
  ): ConditionRule[] {
    const condition = this._conditions[conditionType];
    return condition?.rules ?? [];
  }

  #getConditionOperator(
    conditionType: "visibility" | "required",
  ): string {
    const condition = this._conditions[conditionType];
    return condition?.operator ?? "All";
  }

  #addRule(conditionType: "visibility" | "required") {
    const availableFields = this.#getAvailableFields();
    if (availableFields.length === 0) return;

    const newRule: ConditionRule = {
      fieldAlias: availableFields[0].alias,
      comparison: ConditionComparison.EQUALS,
      value: "",
    };

    const currentCondition = this._conditions[conditionType];
    const newCondition: ConditionDefinition = {
      operator: currentCondition?.operator ?? "All",
      rules: [...(currentCondition?.rules ?? []), newRule],
    };

    this._conditions = {
      ...this._conditions,
      [conditionType]: newCondition,
    };

    this.#dispatchChange();
  }

  #removeRule(conditionType: "visibility" | "required", index: number) {
    const currentCondition = this._conditions[conditionType];
    if (!currentCondition) return;

    const newRules = [...currentCondition.rules];
    newRules.splice(index, 1);

    const newCondition: ConditionDefinition = {
      ...currentCondition,
      rules: newRules,
    };

    this._conditions = {
      ...this._conditions,
      [conditionType]: newCondition,
    };

    this.#dispatchChange();
  }

  #updateRuleField(
    conditionType: "visibility" | "required",
    index: number,
    fieldAlias: string,
  ) {
    const currentCondition = this._conditions[conditionType];
    if (!currentCondition) return;

    const newRules = [...currentCondition.rules];
    newRules[index] = { ...newRules[index], fieldAlias };

    this._conditions = {
      ...this._conditions,
      [conditionType]: { ...currentCondition, rules: newRules },
    };

    this.#dispatchChange();
  }

  #updateRuleComparison(
    conditionType: "visibility" | "required",
    index: number,
    comparison: ConditionComparison,
  ) {
    const currentCondition = this._conditions[conditionType];
    if (!currentCondition) return;

    const newRules = [...currentCondition.rules];
    newRules[index] = { ...newRules[index], comparison };

    this._conditions = {
      ...this._conditions,
      [conditionType]: { ...currentCondition, rules: newRules },
    };

    this.#dispatchChange();
  }

  #updateRuleValue(
    conditionType: "visibility" | "required",
    index: number,
    value: unknown,
  ) {
    const currentCondition = this._conditions[conditionType];
    if (!currentCondition) return;

    const newRules = [...currentCondition.rules];
    newRules[index] = { ...newRules[index], value };

    this._conditions = {
      ...this._conditions,
      [conditionType]: { ...currentCondition, rules: newRules },
    };

    this.#dispatchChange();
  }

  #updateOperator(conditionType: "visibility" | "required", operator: string) {
    const currentCondition = this._conditions[conditionType];
    if (!currentCondition) return;

    this._conditions = {
      ...this._conditions,
      [conditionType]: { ...currentCondition, operator },
    };

    this.#dispatchChange();
  }

  #dispatchChange() {
    this.dispatchEvent(
      new CustomEvent("conditions-change", {
        detail: this._conditions,
        bubbles: true,
        composed: true,
      }),
    );
  }

  #renderRule(
    rule: ConditionRule,
    index: number,
    conditionType: "visibility" | "required",
  ) {
    const availableFields = this.#getAvailableFields();
    const comparisons = Object.values(ConditionComparison);

    const needsValue = ![
      ConditionComparison.IS_EMPTY,
      ConditionComparison.IS_NOT_EMPTY,
    ].includes(rule.comparison);

    return html`
      <div class="rule">
        <select
          class="uui-select"
          .value=${rule.fieldAlias}
          @change=${(e: Event) =>
            this.#updateRuleField(
              conditionType,
              index,
              (e.target as HTMLSelectElement).value,
            )}
        >
          ${availableFields.map(
            (f) =>
              html`<option value=${f.alias} ?selected=${f.alias === rule.fieldAlias}>
                ${f.label}
              </option>`,
          )}
        </select>

        <select
          class="uui-select"
          .value=${rule.comparison}
          @change=${(e: Event) =>
            this.#updateRuleComparison(
              conditionType,
              index,
              (e.target as HTMLSelectElement).value as ConditionComparison,
            )}
        >
          ${comparisons.map(
            (c) =>
              html`<option value=${c} ?selected=${c === rule.comparison}>
                ${c}
              </option>`,
          )}
        </select>

        ${needsValue
          ? html`
              <input
                type="text"
                class="uui-input"
                .value=${rule.value?.toString() ?? ""}
                @input=${(e: Event) =>
                  this.#updateRuleValue(
                    conditionType,
                    index,
                    (e.target as HTMLInputElement).value,
                  )}
                placeholder="Value"
              />
            `
          : null}

        <uui-button
          look="secondary"
          compact
          @click=${() => this.#removeRule(conditionType, index)}
        >
          <uui-icon name="delete"></uui-icon>
        </uui-button>
      </div>
    `;
  }

  #renderConditionSection(conditionType: "visibility" | "required") {
    const rules = this.#getConditionRules(conditionType);
    const operator = this.#getConditionOperator(conditionType);
    const label = conditionType === "visibility" ? "Show this field when" : "Make required when";

    return html`
      <div class="condition-section">
        <h4>${label}</h4>
        <div class="operator">
          <uui-radio
            name="operator-${conditionType}"
            value="All"
            ?checked=${operator === "All"}
            @change=${() => this.#updateOperator(conditionType, "All")}
          >
            All conditions met
          </uui-radio>
          <uui-radio
            name="operator-${conditionType}"
            value="Any"
            ?checked=${operator === "Any"}
            @change=${() => this.#updateOperator(conditionType, "Any")}
          >
            Any condition met
          </uui-radio>
        </div>

        <div class="rules">
          ${rules.map((rule, index) =>
            this.#renderRule(rule, index, conditionType),
          )}
        </div>

        <uui-button
          look="secondary"
          @click=${() => this.#addRule(conditionType)}
        >
          + Add condition
        </uui-button>
      </div>
    `;
  }

  render() {
    const availableFields = this.#getAvailableFields();

    if (availableFields.length === 0) {
      return html`<p class="no-fields">
        No other fields available to create conditions.
      </p>`;
    }

    return html`
      <div class="conditions-editor">
        ${this.#renderConditionSection("visibility")}
        ${this.#renderConditionSection("required")}
      </div>
    `;
  }

  static styles = css`
    .conditions-editor {
      display: flex;
      flex-direction: column;
      gap: 16px;
    }

    .condition-section {
      border: 1px solid var(--uui-color-border);
      padding: 12px;
      border-radius: var(--uui-border-radius);
      overflow: hidden;
    }

    .condition-section h4 {
      margin: 0 0 12px 0;
      font-weight: 600;
      color: var(--uui-color-text);
    }

    .operator {
      display: flex;
      gap: 16px;
      margin-bottom: 12px;
    }

    .operator uui-radio {
      --uui-radio-label-font-size: 13px;
    }

    .rules {
      display: flex;
      flex-direction: column;
      gap: 8px;
      margin-bottom: 12px;
    }

    .rule {
      display: flex;
      gap: 8px;
      align-items: center;
      flex-wrap: wrap;
    }

    .rule select,
    .rule input {
      flex: 1;
      min-width: 100px;
      padding: 6px 10px;
      border: 1px solid var(--uui-color-border);
      border-radius: var(--uui-border-radius);
      background-color: var(--uui-color-surface);
      color: var(--uui-color-text);
      font-size: 14px;
    }

    .rule select:focus,
    .rule input:focus {
      outline: none;
      border-color: var(--uui-color-focus);
    }

    .rule select:hover,
    .rule input:hover {
      border-color: var(--uui-color-border-hover);
    }

    .no-fields {
      color: var(--uui-color-text-alt);
      font-style: italic;
    }
  `;
}
