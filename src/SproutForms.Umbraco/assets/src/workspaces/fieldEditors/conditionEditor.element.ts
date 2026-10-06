import { css, html, LitElement } from "lit";
import { property, state } from "lit/decorators.js";
import { customElement } from "@umbraco-cms/backoffice/external/lit";
import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";
import { FormFieldDto } from "../../models";
import {
  ConditionComparison,
  ConditionDefinition,
  ConditionRule,
  ConditionValueSource,
  FormVariable,
} from "../../api";
import { SF_FORM_DETAIL_TOKEN_CONTEXT } from "../sproutFormsWorkspaceContext";
import "@umbraco-cms/backoffice/external/uui";

const COMPARISON_LABELS: Record<ConditionComparison, string> = {
  [ConditionComparison.EQUALS]: "equals",
  [ConditionComparison.NOT_EQUALS]: "does not equal",
  [ConditionComparison.CONTAINS]: "contains",
  [ConditionComparison.GREATER_THAN]: "is greater than",
  [ConditionComparison.LESS_THAN]: "is less than",
  [ConditionComparison.IS_EMPTY]: "is empty",
  [ConditionComparison.IS_NOT_EMPTY]: "is not empty",
  [ConditionComparison.MATCHES_REGEX]: "matches",
  [ConditionComparison.DOES_NOT_MATCH_REGEX]: "does not match",
};

const COMPARISONS_WITHOUT_VALUE = [ConditionComparison.IS_EMPTY, ConditionComparison.IS_NOT_EMPTY];

// A rule's subject and what it compares with each share one dropdown, so the options say what they point at
const FIELD_PREFIX = "field:";
const VARIABLE_PREFIX = "var:";
const VALUE_OPTION = "value";

export class ConditionChangeEvent extends Event {
  static readonly TYPE = "condition-change";

  constructor(public readonly condition: ConditionDefinition) {
    super(ConditionChangeEvent.TYPE, { bubbles: true, composed: true });
  }
}

export function variableName(variable: FormVariable): string {
  return variable.label || variable.alias;
}

// The variables the condition reads, as the subject of a rule or as what a rule compares with
export function getConditionVariables(condition?: ConditionDefinition | null): string[] {
  return (condition?.rules ?? []).flatMap((rule) => [
    ...(rule.variableAlias ? [rule.variableAlias] : []),
    ...(rule.valueSource === ConditionValueSource.VARIABLE && rule.value ? [String(rule.value)] : []),
  ]);
}

// Reads the condition as a sentence, such as: Capital equals "Paris" and Score is greater than "10"
export function describeCondition(
  condition: ConditionDefinition,
  fields: FormFieldDto[],
  variables: FormVariable[],
): string {
  const fieldName = (alias: string) => fields.find((f) => f.alias === alias)?.label ?? alias;
  const varName = (alias: string) => {
    const variable = variables.find((v) => v.alias === alias);
    return variable ? variableName(variable) : alias;
  };

  return condition.rules
    .map((rule) => {
      const subject = rule.variableAlias ? varName(rule.variableAlias) : fieldName(rule.fieldAlias);
      const comparison = COMPARISON_LABELS[rule.comparison] ?? rule.comparison;
      if (COMPARISONS_WITHOUT_VALUE.includes(rule.comparison)) return `${subject} ${comparison}`;

      const value = String(rule.value ?? "");
      if (rule.valueSource === ConditionValueSource.FIELD) return `${subject} ${comparison} ${fieldName(value)}`;
      if (rule.valueSource === ConditionValueSource.VARIABLE) return `${subject} ${comparison} ${varName(value)}`;
      return `${subject} ${comparison} "${value}"`;
    })
    .join(condition.operator === "Any" ? " or " : " and ");
}

/**
 * Edits one condition: whether all or any of its rules must hold, and the rules, each comparing one of the given fields
 * or one of the form's variables to a value, another field or a variable.
 */
@customElement("sf-condition-editor")
export class ConditionEditor extends UmbElementMixin(LitElement) {
  @property({ type: String })
  public label = "";

  @property({ type: Object })
  public condition?: ConditionDefinition | null;

  // The fields the rules can compare, such as only the top-level fields. The form's variables can always be compared
  @property({ type: Array })
  public fields: FormFieldDto[] = [];

  @state()
  private _variables: FormVariable[] = [];

  constructor() {
    super();
    this.consumeContext(SF_FORM_DETAIL_TOKEN_CONTEXT, (context) => {
      this.observe(context?.variables, (variables) => {
        this._variables = variables ?? [];
      });
    });
  }

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

  // A new rule compares the first field, or the first variable when there are no fields
  #addRule() {
    const firstVariable = this._variables[0];
    if (this.fields.length === 0 && !firstVariable) return;

    this.#change({
      rules: [
        ...this.#rules,
        {
          fieldAlias: this.fields[0]?.alias ?? "",
          variableAlias: this.fields.length === 0 ? firstVariable.alias : null,
          comparison: ConditionComparison.EQUALS,
          value: "",
          valueSource: ConditionValueSource.VALUE,
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

  // A rule that reads a variable has no field alias
  #onSubjectChange(index: number, option: string) {
    if (option.startsWith(VARIABLE_PREFIX)) {
      this.#updateRule(index, { fieldAlias: "", variableAlias: option.substring(VARIABLE_PREFIX.length) });
    } else {
      this.#updateRule(index, { fieldAlias: option.substring(FIELD_PREFIX.length), variableAlias: null });
    }
  }

  // A rule that compares with a field or variable keeps its alias as the value
  #onTargetChange(index: number, option: string) {
    if (option.startsWith(VARIABLE_PREFIX)) {
      this.#updateRule(index, {
        valueSource: ConditionValueSource.VARIABLE,
        value: option.substring(VARIABLE_PREFIX.length),
      });
    } else if (option.startsWith(FIELD_PREFIX)) {
      this.#updateRule(index, {
        valueSource: ConditionValueSource.FIELD,
        value: option.substring(FIELD_PREFIX.length),
      });
    } else {
      this.#updateRule(index, { valueSource: ConditionValueSource.VALUE, value: "" });
    }
  }

  #isAvailable(option: string) {
    if (option.startsWith(VARIABLE_PREFIX)) {
      return this._variables.some((v) => VARIABLE_PREFIX + v.alias === option);
    }
    return this.fields.some((f) => FIELD_PREFIX + f.alias === option);
  }

  #renderOptions(selected: string, excluded?: string) {
    const fields = this.fields.filter((f) => FIELD_PREFIX + f.alias !== excluded);
    const variables = this._variables.filter((v) => VARIABLE_PREFIX + v.alias !== excluded);

    return html`
      ${fields.length > 0
        ? html`<optgroup label="Fields">
            ${fields.map(
              (f) =>
                html`<option value=${FIELD_PREFIX + f.alias} ?selected=${FIELD_PREFIX + f.alias === selected}>
                  ${f.label}
                </option>`,
            )}
          </optgroup>`
        : null}
      ${variables.length > 0
        ? html`<optgroup label="Variables">
            ${variables.map(
              (v) =>
                html`<option value=${VARIABLE_PREFIX + v.alias} ?selected=${VARIABLE_PREFIX + v.alias === selected}>
                  ${variableName(v)}
                </option>`,
            )}
          </optgroup>`
        : null}
    `;
  }

  // What the rule compares with: a typed value, another field or a variable
  #renderTarget(rule: ConditionRule, index: number, subject: string) {
    const target =
      rule.valueSource === ConditionValueSource.FIELD
        ? FIELD_PREFIX + rule.value
        : rule.valueSource === ConditionValueSource.VARIABLE
          ? VARIABLE_PREFIX + rule.value
          : VALUE_OPTION;
    const isUnavailable = target !== VALUE_OPTION && !this.#isAvailable(target);

    return html`
      <select
        class="uui-select ${isUnavailable ? "invalid" : ""}"
        .value=${target}
        @change=${(e: Event) => this.#onTargetChange(index, (e.target as HTMLSelectElement).value)}
      >
        <option value=${VALUE_OPTION} ?selected=${target === VALUE_OPTION}>A value</option>
        ${isUnavailable
          ? html`<option value=${target} selected>
              Unavailable ${rule.valueSource === ConditionValueSource.VARIABLE ? "variable" : "field"}
            </option>`
          : null}
        ${this.#renderOptions(target, subject)}
      </select>

      ${target === VALUE_OPTION
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
    `;
  }

  #renderRule(rule: ConditionRule, index: number) {
    const comparisons = Object.values(ConditionComparison);
    const needsValue = !COMPARISONS_WITHOUT_VALUE.includes(rule.comparison);
    const subject = rule.variableAlias ? VARIABLE_PREFIX + rule.variableAlias : FIELD_PREFIX + rule.fieldAlias;
    // A rule can point at a field that's no longer available, for example after it moved to a later page
    const isUnavailable = !this.#isAvailable(subject);

    return html`
      <div class="rule">
        <select
          class="uui-select ${isUnavailable ? "invalid" : ""}"
          .value=${subject}
          @change=${(e: Event) => this.#onSubjectChange(index, (e.target as HTMLSelectElement).value)}
        >
          ${isUnavailable
            ? html`<option value=${subject} selected>
                Unavailable ${rule.variableAlias ? "variable" : "field"}
              </option>`
            : null}
          ${this.#renderOptions(subject)}
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

        ${needsValue ? this.#renderTarget(rule, index, subject) : null}

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
        ${this.label ? html`<h4>${this.label}</h4>` : null}
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

        <uui-button
          look="secondary"
          ?disabled=${this.fields.length === 0 && this._variables.length === 0}
          @click=${this.#addRule}
        >
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
