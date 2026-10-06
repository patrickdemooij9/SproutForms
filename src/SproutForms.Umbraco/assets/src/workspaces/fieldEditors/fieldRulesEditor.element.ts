import { css, html, LitElement } from "lit";
import { property, state } from "lit/decorators.js";
import { customElement, nothing, repeat } from "@umbraco-cms/backoffice/external/lit";
import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";
import { UUISelectEvent } from "@umbraco-cms/backoffice/external/uui";
import { CalculationRuleDto, FormDefinitionDto, FormFieldDto } from "../../models";
import {
  CalculationOperation,
  ConditionComparison,
  ConditionDefinition,
  ConditionValueSource,
  FieldRule,
  FieldRuleAction,
} from "../../api";
import SproutFormsWorkspaceContext, { SF_FORM_DETAIL_TOKEN_CONTEXT } from "../sproutFormsWorkspaceContext";
import { ConditionChangeEvent } from "./conditionEditor.element";
import { CalculationActionChangeEvent, defaultOperandValue } from "./calculationActionEditor.element";
import "./conditionEditor.element";
import "./calculationActionEditor.element";

// A rule either shows, hides or requires the field, or changes a variable
const CHANGE_VARIABLE = "Variable";
type RuleAction = FieldRuleAction | typeof CHANGE_VARIABLE;

const ACTION_LABELS: Record<RuleAction, string> = {
  [FieldRuleAction.SHOW]: "Show this field",
  [FieldRuleAction.HIDE]: "Hide this field",
  [FieldRuleAction.REQUIRE]: "Make required",
  [CHANGE_VARIABLE]: "Change a variable",
};

// A rule of the field itself, or a calculation it owns, which lives in the form's calculations
type RuleItem =
  | { kind: "field"; index: number; rule: FieldRule }
  | { kind: "calculation"; rule: CalculationRuleDto };

/**
 * The field's rules, each "when <condition> then <action>". Showing, hiding and requiring are stored on the field;
 * a rule that changes a variable is a calculation the field owns, appended to the form's calculations, so it runs in
 * its place in that list.
 */
@customElement("sf-field-rules-editor")
export class FieldRulesEditor extends UmbElementMixin(LitElement) {
  private context?: SproutFormsWorkspaceContext;

  @property({ type: Object })
  public field!: FormFieldDto;

  // The fields its conditions can use: those on the field's own page and earlier pages, and inside a field group the other fields of its entry
  @property({ type: Array })
  public fields: FormFieldDto[] = [];

  // False for a field that can't be required, such as a field group
  @property({ type: Boolean })
  public allowRequired = true;

  @state()
  private definition?: FormDefinitionDto;

  constructor() {
    super();
    this.consumeContext(SF_FORM_DETAIL_TOKEN_CONTEXT, (context) => {
      this.context = context;
      this.observe(context?.form, (form) => {
        this.definition = form?.definition;
      });
    });
  }

  // Calculations may only use, and belong to, the fields that aren't inside a field group
  get #isTopLevel() {
    return !!this.definition?.fields.some((field) => field.id === this.field.id);
  }

  get #canChangeVariables() {
    return this.#isTopLevel && (this.definition?.variables.length ?? 0) > 0;
  }

  get #items(): RuleItem[] {
    return [
      ...this.field.rules.map((rule, index) => ({ kind: "field" as const, index, rule })),
      ...(this.definition?.calculations ?? [])
        .filter((rule) => rule.ownerFieldAlias === this.field.alias)
        .map((rule) => ({ kind: "calculation" as const, rule })),
    ];
  }

  // A rule on a field's answer, such as: this field equals …
  #conditionOn(fieldAlias: string): ConditionDefinition {
    return {
      operator: "All",
      rules: [
        {
          fieldAlias,
          variableAlias: null,
          comparison: ConditionComparison.EQUALS,
          value: "",
          valueSource: ConditionValueSource.VALUE,
        },
      ],
    };
  }

  #newCalculation(condition: ConditionDefinition | null): CalculationRuleDto {
    const variable = this.definition!.variables[0];
    return {
      id: crypto.randomUUID(),
      condition,
      variableAlias: variable.alias,
      operation: CalculationOperation.SET,
      operand: { source: ConditionValueSource.VALUE, value: defaultOperandValue(variable) },
      ownerFieldAlias: this.field.alias,
    };
  }

  #setFieldRules(rules: FieldRule[]) {
    this.context?.updateField({ id: this.field.id, rules });
  }

  #setCalculations(calculations: CalculationRuleDto[]) {
    this.context?.updateCalculations(calculations);
  }

  // Changing a variable is the likeliest use of the field's own answer; a field can't show or hide itself on it,
  // so a show rule starts from another field when there is one
  #addRule() {
    if (this.#canChangeVariables) {
      this.#setCalculations([...this.definition!.calculations, this.#newCalculation(this.#conditionOn(this.field.alias))]);
      return;
    }
    const other = this.fields.find((field) => field.id !== this.field.id) ?? this.field;
    this.#setFieldRules([...this.field.rules, { condition: this.#conditionOn(other.alias), action: FieldRuleAction.SHOW }]);
  }

  #removeRule(item: RuleItem) {
    if (item.kind === "field") {
      this.#setFieldRules(this.field.rules.filter((_, i) => i !== item.index));
    } else {
      this.#setCalculations(this.definition!.calculations.filter((rule) => rule.id !== item.rule.id));
    }
  }

  #updateCondition(item: RuleItem, condition: ConditionDefinition) {
    if (item.kind === "field") {
      this.#setFieldRules(this.field.rules.map((rule, i) => (i === item.index ? { ...rule, condition } : rule)));
    } else {
      this.#updateCalculation(item.rule.id, { condition });
    }
  }

  #updateCalculation(id: string, changes: Partial<CalculationRuleDto>) {
    this.#setCalculations(
      this.definition!.calculations.map((rule) => (rule.id === id ? { ...rule, ...changes } : rule)),
    );
  }

  // A rule that starts or stops changing a variable moves between the field's rules and the form's calculations,
  // keeping its condition
  #changeAction(item: RuleItem, action: RuleAction) {
    if (item.kind === "field") {
      if (action === CHANGE_VARIABLE) {
        this.#setCalculations([...this.definition!.calculations, this.#newCalculation(item.rule.condition)]);
        this.#setFieldRules(this.field.rules.filter((_, i) => i !== item.index));
      } else {
        this.#setFieldRules(this.field.rules.map((rule, i) => (i === item.index ? { ...rule, action } : rule)));
      }
    } else if (action !== CHANGE_VARIABLE) {
      this.#setCalculations(this.definition!.calculations.filter((rule) => rule.id !== item.rule.id));
      this.#setFieldRules([
        ...this.field.rules,
        { condition: item.rule.condition ?? { operator: "All", rules: [] }, action },
      ]);
    }
  }

  // The server rejects a field whose visibility depends on its own answer
  #usesOwnAnswer(condition: ConditionDefinition) {
    return condition.rules.some(
      (rule) =>
        (!rule.variableAlias && rule.fieldAlias === this.field.alias) ||
        (rule.valueSource === ConditionValueSource.FIELD && rule.value === this.field.alias),
    );
  }

  #renderRule(item: RuleItem, number: number) {
    const action: RuleAction = item.kind === "field" ? item.rule.action : CHANGE_VARIABLE;
    // The rule's own action stays listed, so it can be seen and replaced
    const actions = (Object.keys(ACTION_LABELS) as RuleAction[]).filter(
      (it) =>
        it === action ||
        (it === FieldRuleAction.REQUIRE ? this.allowRequired : it === CHANGE_VARIABLE ? this.#canChangeVariables : true),
    );

    return html`
      <div class="rule">
        <div class="rule-header">
          <strong>Rule ${number}</strong>
          <uui-button look="secondary" compact label="Remove rule" @click=${() => this.#removeRule(item)}>
            <uui-icon name="delete"></uui-icon>
          </uui-button>
        </div>

        <sf-condition-editor
          label="When"
          .condition=${item.rule.condition}
          .fields=${this.fields}
          @condition-change=${(e: ConditionChangeEvent) => {
            e.stopPropagation();
            this.#updateCondition(item, e.condition);
          }}
        ></sf-condition-editor>
        <p class="description">Without conditions, the rule always applies.</p>

        <div class="then">
          <uui-label>Then</uui-label>
          <uui-select
            label="Then"
            .options=${actions.map((it) => ({ name: ACTION_LABELS[it], value: it, selected: it === action }))}
            @change=${(e: UUISelectEvent) => this.#changeAction(item, e.target.value as RuleAction)}
          ></uui-select>
        </div>

        ${item.kind === "calculation"
          ? html`
              <sf-calculation-action-editor
                .rule=${item.rule}
                .fields=${this.definition?.fields ?? []}
                @calculation-action-change=${(e: CalculationActionChangeEvent) => {
                  e.stopPropagation();
                  this.#updateCalculation(item.rule.id, e.changes);
                }}
              ></sf-calculation-action-editor>
              <p class="description">Runs in its place among the rules in Settings → Calculations.</p>
            `
          : nothing}
        ${item.kind === "field" && item.rule.action !== FieldRuleAction.REQUIRE && this.#usesOwnAnswer(item.rule.condition)
          ? html`<p class="warning">A field can't be shown or hidden by its own answer.</p>`
          : nothing}
      </div>
    `;
  }

  #renderVariableHint() {
    if (!this.#isTopLevel) {
      return html`<p class="description">Rules that change a variable can only be on fields outside a repeater.</p>`;
    }
    if (!this.#canChangeVariables) {
      return html`<p class="description">Create variables in Settings → Calculations to change them from this field.</p>`;
    }
    return nothing;
  }

  render() {
    const items = this.#items;

    return html`
      <div class="rules-editor">
        <p class="description">
          Hidden while any hide rule holds. With show rules, only shown while one of them holds. Required while any
          required rule holds.
        </p>
        ${repeat(
          items,
          (item) => (item.kind === "field" ? `field-${item.index}` : item.rule.id),
          (item, index) => this.#renderRule(item, index + 1),
        )}
        <uui-button look="secondary" label="Add rule" @click=${this.#addRule}>+ Add rule</uui-button>
        ${this.#renderVariableHint()}
      </div>
    `;
  }

  static styles = css`
    .rules-editor {
      display: flex;
      flex-direction: column;
      gap: 16px;
    }

    .rule {
      display: flex;
      flex-direction: column;
      gap: 12px;
      padding: 12px;
      border: 1px solid var(--uui-color-border);
      border-radius: var(--uui-border-radius);
    }

    .rule-header {
      display: flex;
      align-items: center;
      justify-content: space-between;
    }

    .then {
      display: flex;
      flex-direction: column;
      gap: var(--uui-size-space-1);
    }

    uui-select {
      width: 100%;
    }

    .rules-editor > uui-button {
      align-self: flex-start;
    }

    .description {
      margin: 0;
      font-size: var(--uui-type-small-size);
      color: var(--uui-color-text-alt);
    }

    .warning {
      margin: 0;
      font-size: var(--uui-type-small-size);
      color: var(--uui-color-warning-standalone);
    }
  `;
}
