import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";
import {
  customElement,
  nothing,
  repeat,
  state,
} from "@umbraco-cms/backoffice/external/lit";
import { css, html, LitElement } from "lit";
import {
  UmbPropertyDatasetElement,
  UmbPropertyValueData,
} from "@umbraco-cms/backoffice/property";
import { SproutFormsSource } from "../repositories/sproutFormsSource";
import SproutFormsWorkspaceContext, {
  SF_FORM_DETAIL_TOKEN_CONTEXT,
} from "./sproutFormsWorkspaceContext";
import {
  FormConditionalOutcomeDto,
  FormDefinitionTypeDto,
  FormDto,
  FormOutcomeDto,
  FormOutcomeTypeDto,
} from "../models";
import { ConditionChangeEvent } from "./fieldEditors/conditionEditor.element";
import "./fieldEditors/conditionEditor.element";
import "./formCalculations.element";

enum SettingsTab {
  General,
  Outcomes,
  Calculations,
}

const SETTINGS_TABS: Array<{ state: SettingsTab; label: string; icon: string }> = [
  { state: SettingsTab.General, label: "General", icon: "icon-settings" },
  { state: SettingsTab.Outcomes, label: "Outcomes", icon: "icon-flag" },
  { state: SettingsTab.Calculations, label: "Calculations", icon: "icon-calculator" },
];

@customElement("form-settings")
export class FormSettingsElement extends UmbElementMixin(LitElement) {
  private context?: SproutFormsWorkspaceContext;

  @state()
  form?: FormDto;

  @state()
  _values: Array<UmbPropertyValueData> = [];

  @state()
  outcomes: Array<FormOutcomeTypeDto> = [];

  @state()
  formType?: FormDefinitionTypeDto;

  @state()
  private _activeTab = SettingsTab.General;

  constructor() {
    super();

    new SproutFormsSource(this).getOutcomes().then((resp) => {
      this.outcomes = resp.data;
    });

    this.consumeContext(SF_FORM_DETAIL_TOKEN_CONTEXT, (context) => {
      this.context = context;

      this.observe(context?.formType, (formType) => {
        this.formType = formType;
      });

      context?.form.subscribe((form) => {
        this.form = form;

        this._values = [
          {
            alias: "alias",
            value: this.form.alias,
          },
          {
            alias: "submitLabel",
            value: this.form.definition.submitLabel ?? "",
          },
          {
            alias: "showProgress",
            value: this.form.definition.showProgress,
          },
        ];

        Object.entries(this.form.definition.outcome.configuration).forEach(
          ([key, value]) => {
            this._values.push({
              alias: "outcome-" + key,
              value: value,
            });
          }
        );
      });
    });
  }

  #onPropertyDataChange(e: Event) {
    const value = (e.target as UmbPropertyDatasetElement).value;

    const updateForm: Partial<FormDto> = {};
    updateForm.definition = structuredClone(this.form!.definition);
    value.forEach((item) => {
      if (item.alias == "alias") {
        updateForm.alias = item.value as string;
        this.context?.lockAliasUpdate();
      } else if (item.alias == "submitLabel") {
        updateForm.definition!.submitLabel = (item.value as string) || null;
      } else if (item.alias == "showProgress") {
        updateForm.definition!.showProgress = item.value === true;
      } else if (item.alias.startsWith("outcome-")) {
        const actualAlias = item.alias.replace("outcome-", "");
        if (
          Object.keys(updateForm.definition!.outcome.configuration).includes(
            actualAlias
          )
        ) {
          updateForm.definition!.outcome.configuration[actualAlias] =
            item.value as string;
        }
      }
    });
    this.context?.updateForm(updateForm);
  }

  // A new outcome of the type, with the type's default configuration
  #createOutcome(alias: string): FormOutcomeDto | undefined {
    const outcome = this.outcomes.find((item) => item.alias == alias);
    if (!outcome) {
      return undefined;
    }

    const configuration: Record<string, string> = {};
    outcome.properties.forEach((prop) => {
      configuration[prop.alias] = prop.value as string ?? "";
    });
    return {
      typeAlias: outcome.alias,
      displayName: outcome.displayName,
      configuration: configuration,
    };
  }

  #selectOutcome(alias: string) {
    if (alias === this.form?.definition.outcome.typeAlias) return;
    const outcome = this.#createOutcome(alias);
    if (!outcome) {
      return;
    }

    const clonedDefinition = structuredClone(this.form!.definition);
    clonedDefinition.outcome = outcome;
    this.context?.updateForm({
      definition: clonedDefinition,
    });
  }

  // The selected outcome stays listed even when the form type doesn't allow it, so it can be seen and replaced
  #getAvailableOutcomes(selectedAlias?: string) {
    const formType = this.formType;
    if (!formType) return this.outcomes;

    return this.outcomes.filter(
      (outcome) =>
        formType.allowedOutcomeTypeAliases.includes(outcome.alias) ||
        outcome.alias === selectedAlias
    );
  }

  get #conditionalOutcomes(): FormConditionalOutcomeDto[] {
    return this.form?.definition.conditionalOutcomes ?? [];
  }

  #addConditionalOutcome() {
    const outcome = this.#createOutcome(this.#getAvailableOutcomes()[0]?.alias ?? "");
    if (!outcome) return;

    this.context?.updateConditionalOutcomes([
      ...this.#conditionalOutcomes,
      { id: crypto.randomUUID(), condition: { operator: "All", rules: [] }, outcome },
    ]);
  }

  #removeConditionalOutcome(id: string) {
    this.context?.updateConditionalOutcomes(this.#conditionalOutcomes.filter((it) => it.id !== id));
  }

  #updateConditionalOutcome(id: string, changes: Partial<FormConditionalOutcomeDto>) {
    this.context?.updateConditionalOutcomes(
      this.#conditionalOutcomes.map((it) => (it.id === id ? { ...it, ...changes } : it))
    );
  }

  #moveConditionalOutcome(index: number, direction: -1 | 1) {
    const conditionalOutcomes = [...this.#conditionalOutcomes];
    const target = index + direction;
    if (target < 0 || target >= conditionalOutcomes.length) return;

    [conditionalOutcomes[index], conditionalOutcomes[target]] = [conditionalOutcomes[target], conditionalOutcomes[index]];
    this.context?.updateConditionalOutcomes(conditionalOutcomes);
  }

  #selectConditionalOutcome(conditional: FormConditionalOutcomeDto, alias: string) {
    if (alias === conditional.outcome.typeAlias) return;
    const outcome = this.#createOutcome(alias);
    if (!outcome) return;

    this.#updateConditionalOutcome(conditional.id, { outcome });
  }

  // Each conditional outcome has a dataset of its own; the form's dataset doesn't hear its changes
  #onConditionalOutcomeDataChange(conditional: FormConditionalOutcomeDto, e: Event) {
    const value = (e.target as UmbPropertyDatasetElement).value;
    const configuration = { ...conditional.outcome.configuration };
    value.forEach((item) => {
      if (Object.keys(configuration).includes(item.alias)) {
        configuration[item.alias] = item.value as string;
      }
    });
    this.#updateConditionalOutcome(conditional.id, {
      outcome: { ...conditional.outcome, configuration },
    });
  }

  #renderOutcomeOptions(selectedAlias: string | undefined, onSelect: (alias: string) => void) {
    return html`
      <div class="option-container" role="radiogroup" aria-label="Submit outcome">
        ${repeat(
          this.#getAvailableOutcomes(selectedAlias),
          (item) => item.alias,
          (item) => {
            const isSelected = item.alias === selectedAlias;
            return html`
              <button
                class="option ${isSelected ? "selected" : ""}"
                role="radio"
                aria-checked=${isSelected}
                @click=${() => onSelect(item.alias)}
              >
                <span class="option-check">
                  ${isSelected ? html`<uui-icon name="icon-check"></uui-icon>` : nothing}
                </span>
                <span>${item.displayName}</span>
              </button>
            `;
          }
        )}
      </div>
    `;
  }

  #renderOutcome() {
    const selectedAlias = this.form?.definition.outcome.typeAlias;
    const selected = this.outcomes.find((item) => item.alias === selectedAlias);

    return html`
      <uui-box headline="Submit outcome">
        <p class="box-description">What happens for the visitor after they submit the form.</p>
        ${this.#renderOutcomeOptions(selectedAlias, (alias) => this.#selectOutcome(alias))}
        ${repeat(
          selected?.properties ?? [],
          (prop) => prop.alias,
          (prop) => html`
            <umb-property
              alias=${"outcome-" + prop.alias}
              label=${prop.displayName}
              description=""
              property-editor-ui-alias=${prop.propertyEditor}
              val
            ></umb-property>
          `
        )}
      </uui-box>
    `;
  }

  #renderConditionalOutcome(conditional: FormConditionalOutcomeDto, index: number) {
    const selected = this.outcomes.find((item) => item.alias === conditional.outcome.typeAlias);
    const count = this.#conditionalOutcomes.length;

    return html`
      <div class="conditional-outcome">
        <div class="conditional-outcome-header">
          <strong>${index + 1}. ${selected?.displayName ?? conditional.outcome.displayName}</strong>
          <uui-button-group>
            <uui-button
              look="secondary"
              compact
              label="Move up"
              ?disabled=${index === 0}
              @click=${() => this.#moveConditionalOutcome(index, -1)}
            >
              <uui-icon name="icon-arrow-up"></uui-icon>
            </uui-button>
            <uui-button
              look="secondary"
              compact
              label="Move down"
              ?disabled=${index === count - 1}
              @click=${() => this.#moveConditionalOutcome(index, 1)}
            >
              <uui-icon name="icon-arrow-down"></uui-icon>
            </uui-button>
            <uui-button
              look="secondary"
              color="danger"
              compact
              label="Remove conditional outcome"
              @click=${() => this.#removeConditionalOutcome(conditional.id)}
            >
              <uui-icon name="icon-trash"></uui-icon>
            </uui-button>
          </uui-button-group>
        </div>

        <sf-condition-editor
          label="Use this outcome when"
          .condition=${conditional.condition}
          .fields=${this.form?.definition.fields ?? []}
          @condition-change=${(e: ConditionChangeEvent) => {
            e.stopPropagation();
            this.#updateConditionalOutcome(conditional.id, { condition: e.condition });
          }}
        ></sf-condition-editor>
        ${conditional.condition.rules.length === 0
          ? html`<p class="warning">Add at least one condition, the form can't be saved without one.</p>`
          : nothing}

        ${this.#renderOutcomeOptions(conditional.outcome.typeAlias, (alias) =>
          this.#selectConditionalOutcome(conditional, alias)
        )}
        <umb-property-dataset
          .value=${Object.entries(conditional.outcome.configuration).map(([alias, value]) => ({ alias, value }))}
          @change=${(e: Event) => this.#onConditionalOutcomeDataChange(conditional, e)}
        >
          ${repeat(
            selected?.properties ?? [],
            (prop) => prop.alias,
            (prop) => html`
              <umb-property
                alias=${prop.alias}
                label=${prop.displayName}
                description=""
                property-editor-ui-alias=${prop.propertyEditor}
                val
              ></umb-property>
            `
          )}
        </umb-property-dataset>
      </div>
    `;
  }

  #renderConditionalOutcomes() {
    return html`
      <uui-box headline="Conditional outcomes">
        <p class="box-description">
          Checked from top to bottom after submitting: the first whose condition holds is used, otherwise the submit
          outcome above. Messages and redirect URLs can show a variable with <code>{var:alias}</code>.
        </p>
        <div class="conditional-outcomes">
          ${repeat(
            this.#conditionalOutcomes,
            (conditional) => conditional.id,
            (conditional, index) => this.#renderConditionalOutcome(conditional, index)
          )}
        </div>
        <uui-button look="secondary" label="Add conditional outcome" @click=${this.#addConditionalOutcome}>
          + Add conditional outcome
        </uui-button>
      </uui-box>
    `;
  }

  #renderGeneral() {
    return html`
      <uui-box headline="General">
        <umb-property
          alias="alias"
          label="Alias"
          description="The generated alias for this form. This is used for programmatic implementations."
          property-editor-ui-alias="Umb.PropertyEditorUi.TextBox"
          val
        ></umb-property>
      </uui-box>

      <uui-box headline="Pages">
        <p class="box-description">
          How the form's buttons read, and whether a form with more than one page shows its steps.
          Each page's own title and button labels are set on the Build tab.
        </p>
        <umb-property
          alias="submitLabel"
          label="Submit button"
          description="The text of the submit button. Empty means Submit."
          property-editor-ui-alias="Umb.PropertyEditorUi.TextBox"
          val
        ></umb-property>
        <umb-property
          alias="showProgress"
          label="Show progress"
          description="Shows the page titles as steps above a form with more than one page."
          property-editor-ui-alias="Umb.PropertyEditorUi.Toggle"
          val
        ></umb-property>
      </uui-box>
    `;
  }

  // The calculations edit the form themselves, so they're outside the form's dataset
  #renderTab() {
    if (this._activeTab === SettingsTab.Calculations) {
      return html`<form-calculations></form-calculations>`;
    }

    return html`
      <div class="settings">
        <umb-property-dataset
          .value=${this._values!}
          @change=${this.#onPropertyDataChange}
        >
          ${this._activeTab === SettingsTab.General
            ? this.#renderGeneral()
            : html`${this.#renderOutcome()} ${this.#renderConditionalOutcomes()}`}
        </umb-property-dataset>
      </div>
    `;
  }

  render() {
    return html`
      <uui-tab-group class="sub-tabs">
        ${SETTINGS_TABS.map(
          (tab) => html`
            <uui-tab
              .label=${tab.label}
              ?active=${this._activeTab === tab.state}
              @click=${() => (this._activeTab = tab.state)}
            >
              <umb-icon slot="icon" name=${tab.icon}></umb-icon>
              ${tab.label}
            </uui-tab>
          `
        )}
      </uui-tab-group>
      <div class="sub-view">${this.#renderTab()}</div>
    `;
  }

  static styles = css`
    :host {
      display: flex;
      flex-direction: column;
      background-color: var(--uui-color-background);
    }

    .sub-tabs {
      flex-shrink: 0;
      height: auto;
      --uui-tab-divider: var(--uui-color-border);
      background-color: var(--uui-color-surface);
      border-bottom: 1px solid var(--uui-color-border);
    }

    .sub-view {
      flex: 1;
      min-height: 0;
      overflow-y: auto;
    }

    .settings {
      max-width: 960px;
      margin: 0 auto;
      padding: var(--uui-size-layout-1);
    }

    umb-property-dataset {
      display: flex;
      flex-direction: column;
      gap: var(--uui-size-layout-1);
    }

    .box-description {
      margin: 0 0 var(--uui-size-space-5);
      color: var(--uui-color-text-alt);
    }

    .conditional-outcomes {
      display: flex;
      flex-direction: column;
      gap: var(--uui-size-space-4);
      margin-bottom: var(--uui-size-space-5);
    }

    .conditional-outcomes:empty {
      display: none;
    }

    .conditional-outcome {
      display: flex;
      flex-direction: column;
      gap: var(--uui-size-space-4);
      padding: var(--uui-size-space-5);
      border: 1px solid var(--uui-color-border);
      border-radius: var(--uui-border-radius);
    }

    .conditional-outcome-header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: var(--uui-size-space-3);
    }

    .conditional-outcome .option-container {
      margin-bottom: 0;
    }

    .warning {
      margin: 0;
      font-size: var(--uui-type-small-size);
      color: var(--uui-color-warning-standalone);
    }

    .option-container {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(200px, 1fr));
      gap: var(--uui-size-space-3);
      margin-bottom: var(--uui-size-space-5);
    }

    .option {
      display: flex;
      align-items: center;
      gap: var(--uui-size-space-3);
      padding: var(--uui-size-space-4);
      font: inherit;
      font-weight: 700;
      text-align: left;
      color: var(--uui-color-text);
      background-color: var(--uui-color-surface);
      border: 1px solid var(--uui-color-border);
      border-radius: calc(var(--uui-border-radius) * 2);
      cursor: pointer;
      transition: border-color 120ms, box-shadow 120ms;

      &:hover {
        border-color: var(--uui-color-border-emphasis);
      }

      &.selected {
        border-color: var(--uui-color-selected);
        box-shadow: 0 0 0 1px var(--uui-color-selected);
      }
    }

    .option-check {
      display: flex;
      align-items: center;
      justify-content: center;
      flex-shrink: 0;
      width: 20px;
      height: 20px;
      border-radius: 50%;
      border: 1px solid var(--uui-color-border-emphasis);
      font-size: 12px;
    }

    .option.selected .option-check {
      border-color: var(--uui-color-selected);
      background-color: var(--uui-color-selected);
      color: var(--uui-color-selected-contrast);
    }
  `;
}
