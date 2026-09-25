import { css, html, LitElement } from "lit";
import { property } from "lit/decorators.js";
import { customElement } from "@umbraco-cms/backoffice/external/lit";
import { FormFieldDto } from "../../models";
import { ConditionComparison, ConditionDefinition, ConditionRule } from "../../api";
import "@umbraco-cms/backoffice/external/uui";

export class ConditionChangeEvent extends Event {
  static readonly TYPE = "condition-change";

  constructor(public readonly condition: ConditionDefinition) {
    super(ConditionChangeEvent.TYPE, { bubbles: true, composed: true });
  }
}

/**
 * Edits one condition: whether all or any of its rules must hold, and the rules, each comparing one of the given fields to a value.
 */
@customElement("sf-condition-editor")
export class ConditionEditor extends LitElement {
  @property({ type: String })
  public label = "";

  @property({ type: Object })
  public condition?: ConditionDefinition | null;

  // The fields the rules can compare
  @property({ type: Array })
  public fields: FormFieldDto[] = [];

  get #rules(): ConditionRule[] {
    return this.condition?.rules ?? [];
  }

  get #operator(): string {
    return this.condition?.operator ?? "All";
  }

  #change(changes: Partial<ConditionDefinition>) {
    this.dispatchEvent(
      new ConditionChangeEvent({
        operator: this.#operator,
        rules: this.#rules,
        ...changes,
      }),
    );
  }

  #addRule() {
    if (this.fields.length === 0) return;

    this.#change({
      rules: [
        ...this.#rules,
        {
          fieldAlias: this.fields[0].alias,
          comparison: ConditionComparison.EQUALS,
          value: "",
        },
      ],
    });
  }

  #removeRule(index: number) {
    this.#change({ rules: this.#rules.filter((_, i) => i !== index) });
  }

  #updateRule(index: number, changes: Partial<ConditionRule>) {
    this.#change({
      rules: this.#rules.map((rule, i) => (i === index ? { ...rule, ...changes } : rule)),
    });
  }

  #renderRule(rule: ConditionRule, index: number) {
    const comparisons = Object.values(ConditionComparison);
    const needsValue = ![
      ConditionComparison.IS_EMPTY,
      ConditionComparison.IS_NOT_EMPTY,
    ].includes(rule.comparison);
    // A rule can point at a field that's no longer available, for example after it moved to a later page
    const isUnavailable = !this.fields.some((f) => f.alias === rule.fieldAlias);

    return html`
      <div class="rule">
        <select
          class="uui-select ${isUnavailable ? "invalid" : ""}"
          .value=${rule.fieldAlias}
          @change=${(e: Event) =>
            this.#updateRule(index, { fieldAlias: (e.target as HTMLSelectElement).value })}
        >
          ${isUnavailable
            ? html`<option value=${rule.fieldAlias} selected>Unavailable field</option>`
            : null}
          ${this.fields.map(
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
            this.#updateRule(index, {
              comparison: (e.target as HTMLSelectElement).value as ConditionComparison,
            })}
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
                  this.#updateRule(index, { value: (e.target as HTMLInputElement).value })}
                placeholder="Value"
              />
            `
          : null}

        <uui-button
          look="secondary"
          compact
          label="Remove condition"
          @click=${() => this.#removeRule(index)}
        >
          <uui-icon name="delete"></uui-icon>
        </uui-button>
      </div>
    `;
  }

  render() {
    return html`
      <div class="condition-section">
        <h4>${this.label}</h4>
        <div class="operator">
          <uui-radio
            name="operator"
            value="All"
            ?checked=${this.#operator === "All"}
            @change=${() => this.#change({ operator: "All" })}
          >
            All conditions met
          </uui-radio>
          <uui-radio
            name="operator"
            value="Any"
            ?checked=${this.#operator === "Any"}
            @change=${() => this.#change({ operator: "Any" })}
          >
            Any condition met
          </uui-radio>
        </div>

        <div class="rules">
          ${this.#rules.map((rule, index) => this.#renderRule(rule, index))}
        </div>

        <uui-button look="secondary" @click=${this.#addRule}>
          + Add condition
        </uui-button>
      </div>
    `;
  }

  static styles = css`
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

    .rule select.invalid {
      border-color: var(--uui-color-danger);
      color: var(--uui-color-danger);
    }
  `;
}
