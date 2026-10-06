import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";
import {
  customElement,
  nothing,
  repeat,
  state,
} from "@umbraco-cms/backoffice/external/lit";
import { css, html, LitElement } from "lit";
import {
  UUIInputEvent,
  UUISelectEvent,
  UUIToggleElement,
} from "@umbraco-cms/backoffice/external/uui";
import SproutFormsWorkspaceContext, {
  SF_FORM_DETAIL_TOKEN_CONTEXT,
} from "./sproutFormsWorkspaceContext";
import { CalculationRuleDto, FormDefinitionDto, FormVariableDto } from "../models";
import { CalculationOperation, ConditionValueSource, FormVariableType } from "../api";
import {
  ConditionChangeEvent,
  describeCondition,
  getConditionVariables,
  variableName,
} from "./fieldEditors/conditionEditor.element";
import {
  CalculationActionChangeEvent,
  defaultOperandValue,
  describeCalculationAction,
  toNumber,
} from "./fieldEditors/calculationActionEditor.element";
import "./fieldEditors/conditionEditor.element";
import "./fieldEditors/calculationActionEditor.element";

const ALIAS_PATTERN = /^[A-Za-z_][A-Za-z0-9_-]*$/;

/**
 * The form's variables, and the rules that change them from the answers. The rules run top to bottom after every
 * answer in the browser, and again on the server, which always has the last word. A rule a field owns is edited on
 * the field's Rules tab too, but runs in its place in this list.
 */
@customElement("form-calculations")
export class FormCalculationsElement extends UmbElementMixin(LitElement) {
  private context?: SproutFormsWorkspaceContext;

  @state()
  private definition?: FormDefinitionDto;

  // The alias of the variable whose rules are shown, or empty for all rules
  @state()
  private _filter = "";

  constructor() {
    super();
    this.consumeContext(SF_FORM_DETAIL_TOKEN_CONTEXT, (context) => {
      this.context = context;
      this.observe(context?.form, (form) => {
        this.definition = form?.definition;
      });
    });
  }

  get #variables(): FormVariableDto[] {
    return this.definition?.variables ?? [];
  }

  get #calculations(): CalculationRuleDto[] {
    return this.definition?.calculations ?? [];
  }

  #getVariable(alias: string) {
    return this.#variables.find((variable) => variable.alias === alias);
  }

  #addVariable() {
    let number = this.#variables.length + 1;
    while (this.#getVariable(`variable${number}`)) number++;

    this.context?.updateVariables([
      ...this.#variables,
      {
        id: crypto.randomUUID(),
        alias: `variable${number}`,
        label: null,
        type: FormVariableType.NUMBER,
        initialValue: 0,
        decimals: 0,
      },
    ]);
  }

  // Rules and conditions that use the variable keep its alias, so the server's validation points them out
  #removeVariable(id: string) {
    this.context?.updateVariables(this.#variables.filter((variable) => variable.id !== id));
  }

  #updateVariable(id: string, changes: Partial<FormVariableDto>) {
    this.context?.updateVariables(
      this.#variables.map((variable) => (variable.id === id ? { ...variable, ...changes } : variable)),
    );
  }

  // The initial value keeps what it can of the old one
  #changeVariableType(variable: FormVariableDto, type: FormVariableType) {
    if (type === variable.type) return;

    const initialValue = variable.initialValue ?? null;
    this.#updateVariable(variable.id, {
      type,
      initialValue: type === FormVariableType.NUMBER ? toNumber(initialValue) : initialValue === null ? null : String(initialValue),
    });
  }

  #getAliasError(variable: FormVariableDto) {
    if (!ALIAS_PATTERN.test(variable.alias)) {
      return "Starts with a letter or underscore, followed by letters, digits, underscores or hyphens.";
    }
    if (this.#variables.some((other) => other.id !== variable.id && other.alias === variable.alias)) {
      return "Another variable has this alias.";
    }
    return undefined;
  }

  // Variables stay on the server, except those a page or field condition reads, and the variables their rules read,
  // since the browser needs them to show and hide the form's parts
  #getVariablesSentToBrowser(): Set<string> {
    const definition = this.definition;
    if (!definition) return new Set();

    const fields = definition.fields.flatMap((field) => [field, ...(field.fields ?? [])]);
    const pending = [
      ...fields.flatMap((field) => field.rules.flatMap((rule) => getConditionVariables(rule.condition))),
      ...definition.pages.flatMap((page) => getConditionVariables(page.visibility)),
    ];

    const used = new Set<string>();
    while (pending.length > 0) {
      const alias = pending.pop()!;
      if (used.has(alias)) continue;
      used.add(alias);

      definition.calculations
        .filter((rule) => rule.variableAlias === alias)
        .forEach((rule) => {
          pending.push(...getConditionVariables(rule.condition));
          if (rule.operand.source === ConditionValueSource.VARIABLE && rule.operand.value) {
            pending.push(String(rule.operand.value));
          }
        });
    }
    return used;
  }

  // The filter only applies while its variable exists
  #getVisibleRules(): CalculationRuleDto[] {
    if (!this._filter || !this.#getVariable(this._filter)) return this.#calculations;
    return this.#calculations.filter((rule) => rule.variableAlias === this._filter);
  }

  #addRule() {
    const variable = this.#getVariable(this._filter) ?? this.#variables[0];
    if (!variable) return;

    this.context?.updateCalculations([
      ...this.#calculations,
      {
        id: crypto.randomUUID(),
        condition: null,
        variableAlias: variable.alias,
        operation: CalculationOperation.SET,
        operand: { source: ConditionValueSource.VALUE, value: defaultOperandValue(variable) },
      },
    ]);
  }

  #removeRule(id: string) {
    this.context?.updateCalculations(this.#calculations.filter((rule) => rule.id !== id));
  }

  #updateRule(id: string, changes: Partial<CalculationRuleDto>) {
    this.context?.updateCalculations(
      this.#calculations.map((rule) => (rule.id === id ? { ...rule, ...changes } : rule)),
    );
  }

  // Swaps the rule with the one above or below it in the list as shown, so moving works while filtered too
  #moveRule(id: string, direction: -1 | 1) {
    const visible = this.#getVisibleRules();
    const neighbour = visible[visible.findIndex((rule) => rule.id === id) + direction];
    if (!neighbour) return;

    const calculations = [...this.#calculations];
    const from = calculations.findIndex((rule) => rule.id === id);
    const to = calculations.findIndex((rule) => rule.id === neighbour.id);
    [calculations[from], calculations[to]] = [calculations[to], calculations[from]];
    this.context?.updateCalculations(calculations);
  }

  // Such as: When Capital equals "Paris" → add 10 to Score
  #describeRule(rule: CalculationRuleDto) {
    const fields = this.definition?.fields ?? [];
    const condition = rule.condition;
    const when = condition && condition.rules.length > 0
      ? `When ${describeCondition(condition, fields, this.#variables)}`
      : "Always";
    return `${when} → ${describeCalculationAction(rule, fields, this.#variables)}`;
  }

  #renderVariable(variable: FormVariableDto, sentToBrowser: Set<string>) {
    const aliasError = this.#getAliasError(variable);
    const isNumber = variable.type === FormVariableType.NUMBER;

    return html`
      <div class="item">
        <div class="variable-grid">
          <div class="property">
            <uui-label for=${`alias-${variable.id}`} required>Alias</uui-label>
            <uui-input
              id=${`alias-${variable.id}`}
              label="Alias"
              .value=${variable.alias}
              ?error=${!!aliasError}
              @input=${(e: UUIInputEvent) => this.#updateVariable(variable.id, { alias: e.target.value as string })}
            ></uui-input>
            ${aliasError ? html`<span class="error">${aliasError}</span>` : nothing}
          </div>
          <div class="property">
            <uui-label for=${`label-${variable.id}`}>Label</uui-label>
            <uui-input
              id=${`label-${variable.id}`}
              label="Label"
              placeholder=${variable.alias}
              .value=${variable.label ?? ""}
              @input=${(e: UUIInputEvent) =>
                this.#updateVariable(variable.id, { label: (e.target.value as string) || null })}
            ></uui-input>
          </div>
          <div class="property">
            <uui-label>Type</uui-label>
            <uui-select
              label="Type"
              .options=${Object.values(FormVariableType).map((type) => ({
                name: type,
                value: type,
                selected: type === variable.type,
              }))}
              @change=${(e: UUISelectEvent) => this.#changeVariableType(variable, e.target.value as FormVariableType)}
            ></uui-select>
          </div>
          <div class="property">
            <uui-label for=${`initial-${variable.id}`}>Initial value</uui-label>
            <uui-input
              id=${`initial-${variable.id}`}
              label="Initial value"
              type=${isNumber ? "number" : "text"}
              .value=${variable.initialValue?.toString() ?? ""}
              @change=${(e: UUIInputEvent) => {
                const value = e.target.value as string;
                this.#updateVariable(variable.id, { initialValue: isNumber ? toNumber(value) : value || null });
              }}
            ></uui-input>
          </div>
          ${isNumber
            ? html`
                <div class="property">
                  <uui-label for=${`decimals-${variable.id}`}>Decimals</uui-label>
                  <uui-input
                    id=${`decimals-${variable.id}`}
                    label="Decimals"
                    type="number"
                    min="0"
                    max="10"
                    .value=${variable.decimals.toString()}
                    @change=${(e: UUIInputEvent) => {
                      const decimals = Math.round(Number(e.target.value) || 0);
                      this.#updateVariable(variable.id, { decimals: Math.min(Math.max(decimals, 0), 10) });
                    }}
                  ></uui-input>
                </div>
              `
            : nothing}
        </div>

        ${sentToBrowser.has(variable.alias)
          ? html`
              <p class="warning">
                <uui-icon name="icon-alert"></uui-icon>
                Used by a field or page condition, so it and its calculations are sent to the browser.
              </p>
            `
          : nothing}

        <uui-button
          class="remove"
          look="secondary"
          color="danger"
          compact
          label="Remove variable"
          @click=${() => this.#removeVariable(variable.id)}
        >
          <uui-icon name="icon-trash"></uui-icon>
        </uui-button>
      </div>
    `;
  }

  #renderVariables() {
    const sentToBrowser = this.#getVariablesSentToBrowser();

    return html`
      <uui-box headline="Variables">
        <p class="box-description">
          Named values the rules below work out from the answers, such as a score or a price. Conditions can use them,
          and messages, redirect URLs and workflows can show them with <code>{var:alias}</code>.
        </p>
        <div class="items">
          ${repeat(
            this.#variables,
            (variable) => variable.id,
            (variable) => this.#renderVariable(variable, sentToBrowser),
          )}
        </div>
        <uui-button look="secondary" label="Add variable" @click=${this.#addVariable}>
          + Add variable
        </uui-button>
      </uui-box>
    `;
  }

  #renderRule(rule: CalculationRuleDto, index: number, count: number) {
    const hasCondition = !!rule.condition;
    // Only a field that isn't inside a field group can own rules
    const owner = this.definition?.fields.find((field) => field.alias === rule.ownerFieldAlias);

    return html`
      <div class="item rule">
        <div class="rule-header">
          <span class="rule-number">${this.#calculations.indexOf(rule) + 1}</span>
          <span class="rule-summary">${this.#describeRule(rule)}</span>
          ${rule.ownerFieldAlias
            ? html`<uui-tag look="secondary" title="Also edited on the field's Rules tab">
                Field: ${owner?.label ?? rule.ownerFieldAlias}
              </uui-tag>`
            : nothing}
          <uui-button-group>
            <uui-button
              look="secondary"
              compact
              label="Move up"
              ?disabled=${index === 0}
              @click=${() => this.#moveRule(rule.id, -1)}
            >
              <uui-icon name="icon-arrow-up"></uui-icon>
            </uui-button>
            <uui-button
              look="secondary"
              compact
              label="Move down"
              ?disabled=${index === count - 1}
              @click=${() => this.#moveRule(rule.id, 1)}
            >
              <uui-icon name="icon-arrow-down"></uui-icon>
            </uui-button>
            <uui-button
              look="secondary"
              color="danger"
              compact
              label="Remove rule"
              @click=${() => this.#removeRule(rule.id)}
            >
              <uui-icon name="icon-trash"></uui-icon>
            </uui-button>
          </uui-button-group>
        </div>

        <sf-calculation-action-editor
          .rule=${rule}
          .fields=${this.definition?.fields ?? []}
          @calculation-action-change=${(e: CalculationActionChangeEvent) => {
            e.stopPropagation();
            this.#updateRule(rule.id, e.changes);
          }}
        ></sf-calculation-action-editor>

        <uui-toggle
          label="Only when"
          ?checked=${hasCondition}
          @change=${(e: Event) =>
            this.#updateRule(rule.id, {
              condition: (e.target as UUIToggleElement).checked ? { operator: "All", rules: [] } : null,
            })}
        >
          Only when…
        </uui-toggle>
        ${hasCondition
          ? html`
              <sf-condition-editor
                .condition=${rule.condition}
                .fields=${this.definition?.fields ?? []}
                @condition-change=${(e: ConditionChangeEvent) => {
                  e.stopPropagation();
                  this.#updateRule(rule.id, { condition: e.condition });
                }}
              ></sf-condition-editor>
            `
          : nothing}
      </div>
    `;
  }

  #renderRules() {
    const visible = this.#getVisibleRules();
    const filter = this.#getVariable(this._filter) ? this._filter : "";

    return html`
      <uui-box headline="Rules">
        <p class="box-description">
          The rules run from top to bottom, so a rule sees what the rules above it worked out. Each changes one
          variable, always or only when its condition holds. Rules can use the fields that aren't inside a repeater.
          A field's own rules are marked with the field, and can be edited on its Rules tab as well.
        </p>
        ${this.#variables.length === 0
          ? html`<p class="empty">Add a variable first, then add the rules that change it.</p>`
          : html`
              <div class="filter">
                <uui-label>Show rules for</uui-label>
                <uui-select
                  label="Show rules for"
                  .options=${[
                    { name: "All variables", value: "", selected: filter === "" },
                    ...this.#variables.map((variable) => ({
                      name: variableName(variable),
                      value: variable.alias,
                      selected: variable.alias === filter,
                    })),
                  ]}
                  @change=${(e: UUISelectEvent) => (this._filter = e.target.value as string)}
                ></uui-select>
              </div>
              <div class="items">
                ${repeat(
                  visible,
                  (rule) => rule.id,
                  (rule, index) => this.#renderRule(rule, index, visible.length),
                )}
              </div>
              <uui-button look="secondary" label="Add rule" @click=${this.#addRule}>
                + Add rule
              </uui-button>
            `}
      </uui-box>
    `;
  }

  render() {
    if (!this.definition) return nothing;

    return html`
      <div class="calculations">
        ${this.#renderVariables()}
        ${this.#renderRules()}
      </div>
    `;
  }

  static styles = css`
    :host {
      display: block;
      background-color: var(--uui-color-background);
    }

    .calculations {
      display: flex;
      flex-direction: column;
      gap: var(--uui-size-layout-1);
      max-width: 960px;
      margin: 0 auto;
      padding: var(--uui-size-layout-1);
    }

    .box-description {
      margin: 0 0 var(--uui-size-space-5);
      color: var(--uui-color-text-alt);
    }

    .empty {
      margin: 0;
      color: var(--uui-color-text-alt);
      font-style: italic;
    }

    .items {
      display: flex;
      flex-direction: column;
      gap: var(--uui-size-space-4);
      margin-bottom: var(--uui-size-space-5);
    }

    .items:empty {
      display: none;
    }

    .item {
      position: relative;
      display: flex;
      flex-direction: column;
      gap: var(--uui-size-space-4);
      padding: var(--uui-size-space-5);
      border: 1px solid var(--uui-color-border);
      border-radius: var(--uui-border-radius);
      background-color: var(--uui-color-surface);
    }

    .variable-grid {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(160px, 1fr));
      gap: var(--uui-size-space-4);
      padding-right: var(--uui-size-space-6);
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

    .error {
      font-size: var(--uui-type-small-size);
      color: var(--uui-color-danger);
    }

    .warning {
      display: flex;
      align-items: center;
      gap: var(--uui-size-space-2);
      margin: 0;
      font-size: var(--uui-type-small-size);
      color: var(--uui-color-warning-standalone);
    }

    .remove {
      position: absolute;
      top: var(--uui-size-space-3);
      right: var(--uui-size-space-3);
    }

    .filter {
      display: flex;
      align-items: center;
      gap: var(--uui-size-space-3);
      margin-bottom: var(--uui-size-space-5);
    }

    .filter uui-select {
      width: auto;
      min-width: 200px;
    }

    .rule-header {
      display: flex;
      align-items: center;
      gap: var(--uui-size-space-3);
    }

    .rule-number {
      display: flex;
      align-items: center;
      justify-content: center;
      flex-shrink: 0;
      width: 24px;
      height: 24px;
      border-radius: 50%;
      background-color: var(--uui-color-surface-alt);
      font-size: var(--uui-type-small-size);
      font-weight: 700;
    }

    .rule-summary {
      flex: 1;
      min-width: 0;
      font-weight: 700;
    }
  `;
}
