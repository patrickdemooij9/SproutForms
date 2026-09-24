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
  FormDefinitionTypeDto,
  FormDto,
  FormOutcomeTypeDto,
} from "../models";

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
  formTypes: Array<FormDefinitionTypeDto> = [];

  @state()
  formType?: FormDefinitionTypeDto;

  constructor() {
    super();

    new SproutFormsSource(this).getOutcomes().then((resp) => {
      this.outcomes = resp.data;
    });

    this.consumeContext(SF_FORM_DETAIL_TOKEN_CONTEXT, (context) => {
      this.context = context;

      this.observe(context?.formTypes, (formTypes) => {
        this.formTypes = formTypes ?? [];
      });
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
        ];

        Object.entries(this.form.definition.type.settings).forEach(
          ([key, value]) => {
            this._values.push({
              alias: "type-" + key,
              value: value,
            });
          }
        );

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
      } else if (item.alias.startsWith("type-")) {
        const actualAlias = item.alias.replace("type-", "");
        if (
          Object.keys(updateForm.definition!.type.settings).includes(
            actualAlias
          )
        ) {
          updateForm.definition!.type.settings[actualAlias] = item.value;
        }
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

  #selectOutcome(alias: string) {
    if (alias === this.form?.definition.outcome.typeAlias) return;
    const outcome = this.outcomes.find((item) => item.alias == alias);
    if (!outcome) {
      return;
    }

    const clonedDefinition = structuredClone(this.form!.definition);
    const configuration: Record<string, string> = {};
    outcome.properties.forEach((prop) => {
      configuration[prop.alias] = prop.value as string ?? "";
    });
    clonedDefinition.outcome = {
      typeAlias: outcome.alias,
      displayName: outcome.displayName,
      configuration: configuration,
    };
    this.context?.updateForm({
      definition: clonedDefinition,
    });
  }

  // The selected outcome stays listed even when the form type doesn't allow it, so it can be seen and replaced
  #getAvailableOutcomes() {
    const formType = this.formType;
    if (!formType) return this.outcomes;

    return this.outcomes.filter(
      (outcome) =>
        formType.allowedOutcomeTypeAliases.includes(outcome.alias) ||
        outcome.alias === this.form?.definition.outcome.typeAlias
    );
  }

  #renderFormType() {
    if (!this.form) return nothing;

    // A standard form is all there is until another form type is registered
    const type = this.form.definition.type;
    if (this.formTypes.length < 2 && type.typeAlias === "standard") return nothing;

    return html`
      <uui-box>
        <div slot="headline" class="headline">
          ${type.displayName}
          <uui-tag look="secondary">Form type</uui-tag>
        </div>
        <p class="box-description">
          ${this.formType?.description ?? ""}
          The type is chosen when the form is created, and can't be changed.
        </p>
        ${repeat(
          this.formType?.properties ?? [],
          (prop) => prop.alias,
          (prop) => html`
            <umb-property
              alias=${"type-" + prop.alias}
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

  #renderOutcome() {
    const selectedAlias = this.form?.definition.outcome.typeAlias;
    const selected = this.outcomes.find((item) => item.alias === selectedAlias);

    return html`
      <uui-box headline="Submit outcome">
        <p class="box-description">What happens for the visitor after they submit the form.</p>
        <div class="option-container" role="radiogroup" aria-label="Submit outcome">
          ${repeat(
            this.#getAvailableOutcomes(),
            (item) => item.alias,
            (item) => {
              const isSelected = item.alias === selectedAlias;
              return html`
                <button
                  class="option ${isSelected ? "selected" : ""}"
                  role="radio"
                  aria-checked=${isSelected}
                  @click=${() => this.#selectOutcome(item.alias)}
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

  render() {
    return html`
      <div class="settings">
        <umb-property-dataset
          .value=${this._values!}
          @change=${this.#onPropertyDataChange}
        >
          <uui-box headline="General">
            <umb-property
              alias="alias"
              label="Alias"
              description="The generated alias for this form. This is used for programmatic implementations."
              property-editor-ui-alias="Umb.PropertyEditorUi.TextBox"
              val
            ></umb-property>
          </uui-box>

          ${this.#renderFormType()}
          ${this.#renderOutcome()}
        </umb-property-dataset>
      </div>
    `;
  }

  static styles = css`
    :host {
      display: block;
      overflow-y: auto;
      background-color: var(--uui-color-background);
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

    .headline {
      display: flex;
      align-items: center;
      gap: var(--uui-size-space-3);
    }

    .box-description {
      margin: 0 0 var(--uui-size-space-5);
      color: var(--uui-color-text-alt);
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
