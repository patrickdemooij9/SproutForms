import { css, html, LitElement } from "lit";
import { property, state } from "lit/decorators.js";
import { customElement } from "@umbraco-cms/backoffice/external/lit";
import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";
import { UUIInputEvent, UUISelectEvent } from "@umbraco-cms/backoffice/external/uui";
import { FormFieldDto } from "../../models";
import {
  CalculationOperand,
  CalculationOperation,
  CalculationRule,
  ConditionValueSource,
  FormVariable,
  FormVariableType,
} from "../../api";
import { SF_FORM_DETAIL_TOKEN_CONTEXT } from "../sproutFormsWorkspaceContext";
import { variableName } from "./conditionEditor.element";

const OPERATION_LABELS: Record<CalculationOperation, string> = {
  [CalculationOperation.SET]: "Set to",
  [CalculationOperation.ADD]: "Add",
  [CalculationOperation.SUBTRACT]: "Subtract",
  [CalculationOperation.MULTIPLY]: "Multiply by",
  [CalculationOperation.DIVIDE]: "Divide by",
  [CalculationOperation.APPEND]: "Append",
};

// The server rejects a rule whose operation doesn't fit its variable's type
const OPERATIONS_BY_TYPE: Record<FormVariableType, CalculationOperation[]> = {
  [FormVariableType.NUMBER]: [
    CalculationOperation.SET,
    CalculationOperation.ADD,
    CalculationOperation.SUBTRACT,
    CalculationOperation.MULTIPLY,
    CalculationOperation.DIVIDE,
  ],
  [FormVariableType.TEXT]: [CalculationOperation.SET, CalculationOperation.APPEND],
};

const OPERAND_SOURCE_LABELS: Record<ConditionValueSource, string> = {
  [ConditionValueSource.VALUE]: "Value",
  [ConditionValueSource.FIELD]: "Field",
  [ConditionValueSource.VARIABLE]: "Variable",
};

export class CalculationActionChangeEvent extends Event {
  static readonly TYPE = "calculation-action-change";

  constructor(public readonly changes: Partial<CalculationRule>) {
    super(CalculationActionChangeEvent.TYPE, { bubbles: true, composed: true });
  }
}

export function defaultOperandValue(variable: FormVariable | undefined): unknown {
  return variable?.type === FormVariableType.NUMBER ? 0 : "";
}

export function toNumber(value: unknown): number | null {
  if (value === null || value === undefined || value === "") return null;
  const number = Number(value);
  return Number.isNaN(number) ? null : number;
}

// Reads what the rule does as a sentence, such as: add 10 to Score
export function describeCalculationAction(
  rule: CalculationRule,
  fields: FormFieldDto[],
  variables: FormVariable[],
): string {
  const varName = (alias: string) => {
    const variable = variables.find((v) => v.alias === alias);
    return variable ? variableName(variable) : alias;
  };
  const describeOperand = (operand: CalculationOperand) => {
    const value = String(operand.value ?? "");
    if (operand.source === ConditionValueSource.FIELD) return fields.find((f) => f.alias === value)?.label ?? value;
    if (operand.source === ConditionValueSource.VARIABLE) return varName(value);
    return typeof operand.value === "number" ? value : `"${value}"`;
  };

  const variable = varName(rule.variableAlias);
  const operand = describeOperand(rule.operand);
  const actions: Record<CalculationOperation, string> = {
    [CalculationOperation.SET]: `set ${variable} to ${operand}`,
    [CalculationOperation.ADD]: `add ${operand} to ${variable}`,
    [CalculationOperation.SUBTRACT]: `subtract ${operand} from ${variable}`,
    [CalculationOperation.MULTIPLY]: `multiply ${variable} by ${operand}`,
    [CalculationOperation.DIVIDE]: `divide ${variable} by ${operand}`,
    [CalculationOperation.APPEND]: `append ${operand} to ${variable}`,
  };
  return actions[rule.operation] ?? rule.operation;
}

/**
 * Edits what a calculation rule does: its variable, the operation, which fits the variable's type, and the value,
 * field or variable it works with.
 */
@customElement("sf-calculation-action-editor")
export class CalculationActionEditor extends UmbElementMixin(LitElement) {
  @property({ type: Object })
  public rule!: CalculationRule;

  // The fields the rule can work with; calculations can only use the fields that aren't inside a field group
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

  #getVariable(alias: string) {
    return this._variables.find((variable) => variable.alias === alias);
  }

  #change(changes: Partial<CalculationRule>) {
    this.dispatchEvent(new CalculationActionChangeEvent(changes));
  }

  // A variable of another type may not allow the rule's operation, and a typed value may no longer fit
  #changeVariable(alias: string) {
    const variable = this.#getVariable(alias);
    if (!variable) return;

    const operations = OPERATIONS_BY_TYPE[variable.type];
    const typeChanged = this.#getVariable(this.rule.variableAlias)?.type !== variable.type;
    this.#change({
      variableAlias: alias,
      operation: operations.includes(this.rule.operation) ? this.rule.operation : CalculationOperation.SET,
      operand:
        typeChanged && this.rule.operand.source === ConditionValueSource.VALUE
          ? { source: ConditionValueSource.VALUE, value: defaultOperandValue(variable) }
          : this.rule.operand,
    });
  }

  // A field or variable operand keeps its alias as the value
  #changeOperandSource(source: ConditionValueSource) {
    if (source === this.rule.operand.source) return;

    let value = defaultOperandValue(this.#getVariable(this.rule.variableAlias));
    if (source === ConditionValueSource.FIELD) value = this.fields[0]?.alias ?? "";
    if (source === ConditionValueSource.VARIABLE) value = this._variables[0]?.alias ?? "";
    this.#change({ operand: { source, value } });
  }

  #renderOperand() {
    const variable = this.#getVariable(this.rule.variableAlias);
    const { source, value } = this.rule.operand;

    if (source === ConditionValueSource.FIELD || source === ConditionValueSource.VARIABLE) {
      const choices =
        source === ConditionValueSource.FIELD
          ? this.fields.map((field) => ({ name: field.label, value: field.alias }))
          : this._variables.map((it) => ({ name: variableName(it), value: it.alias }));
      const isUnavailable = !choices.some((choice) => choice.value === value);

      return html`
        <uui-select
          label=${OPERAND_SOURCE_LABELS[source]}
          ?error=${isUnavailable}
          .options=${[
            ...(isUnavailable
              ? [{ name: `Unavailable ${source === ConditionValueSource.FIELD ? "field" : "variable"}`, value: String(value ?? ""), selected: true }]
              : []),
            ...choices.map((choice) => ({ ...choice, selected: choice.value === value })),
          ]}
          @change=${(e: UUISelectEvent) => this.#change({ operand: { source, value: e.target.value } })}
        ></uui-select>
      `;
    }

    // A number is only read once it's typed, as a half-typed number such as "1." isn't one yet
    const isNumber = variable?.type === FormVariableType.NUMBER;
    return html`
      <uui-input
        label="Value"
        placeholder="Value"
        type=${isNumber ? "number" : "text"}
        .value=${value?.toString() ?? ""}
        @change=${(e: UUIInputEvent) => {
          const input = e.target.value as string;
          this.#change({ operand: { source, value: isNumber ? toNumber(input) : input } });
        }}
      ></uui-input>
    `;
  }

  render() {
    const variable = this.#getVariable(this.rule.variableAlias);
    const operations = variable ? OPERATIONS_BY_TYPE[variable.type] : Object.values(CalculationOperation);
    const isOperationAllowed = operations.includes(this.rule.operation);

    return html`
      <div class="property">
        <uui-label>Variable</uui-label>
        <uui-select
          label="Variable"
          ?error=${!variable}
          .options=${[
            ...(variable ? [] : [{ name: "Unavailable variable", value: this.rule.variableAlias, selected: true }]),
            ...this._variables.map((it) => ({
              name: variableName(it),
              value: it.alias,
              selected: it.alias === this.rule.variableAlias,
            })),
          ]}
          @change=${(e: UUISelectEvent) => this.#changeVariable(e.target.value as string)}
        ></uui-select>
      </div>
      <div class="property">
        <uui-label>Operation</uui-label>
        <uui-select
          label="Operation"
          ?error=${!isOperationAllowed}
          .options=${[
            ...(isOperationAllowed
              ? []
              : [{ name: `${OPERATION_LABELS[this.rule.operation]} (not for this type)`, value: this.rule.operation, selected: true }]),
            ...operations.map((operation) => ({
              name: OPERATION_LABELS[operation],
              value: operation,
              selected: operation === this.rule.operation,
            })),
          ]}
          @change=${(e: UUISelectEvent) => this.#change({ operation: e.target.value as CalculationOperation })}
        ></uui-select>
      </div>
      <div class="property">
        <uui-label>From</uui-label>
        <uui-select
          label="From"
          .options=${Object.values(ConditionValueSource).map((source) => ({
            name: OPERAND_SOURCE_LABELS[source],
            value: source,
            selected: source === this.rule.operand.source,
          }))}
          @change=${(e: UUISelectEvent) => this.#changeOperandSource(e.target.value as ConditionValueSource)}
        ></uui-select>
      </div>
      <div class="property">
        <uui-label>${OPERAND_SOURCE_LABELS[this.rule.operand.source]}</uui-label>
        ${this.#renderOperand()}
      </div>
    `;
  }

  static styles = css`
    :host {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(160px, 1fr));
      gap: var(--uui-size-space-4);
    }

    .property {
      display: flex;
      flex-direction: column;
      gap: var(--uui-size-space-1);
      min-width: 0;
    }

    uui-input,
    uui-select {
      width: 100%;
    }
  `;
}
