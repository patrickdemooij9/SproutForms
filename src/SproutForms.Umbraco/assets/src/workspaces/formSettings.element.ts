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
import { UmbChangeEvent } from "@umbraco-cms/backoffice/event";
import { UUIRadioElement } from "@umbraco-cms/backoffice/external/uui";
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

  #onOutcomeChange(e: UmbChangeEvent) {
    const value = (e.target as UUIRadioElement).value;
    const outcome = this.outcomes.find((item) => item.alias == value);
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
      <umb-property-layout
        label="Form type"
        description="The kind of form this is. It is chosen when the form is created, and can't be changed."
      >
        <div slot="editor" class="option">
          <h3>${type.displayName}</h3>
          ${this.formType
            ? html`<p class="option-description">${this.formType.description}</p>`
            : nothing}
          ${repeat(
            this.formType?.properties ?? [],
            (prop) => prop.alias,
            (prop) => html`
              <umb-property
                alias=${"type-" + prop.alias}
                label=${prop.displayName}
                description=""
                property-editor-ui-alias=${prop.propertyEditor}
                .appearance=${{
                  labelOnTop: true,
                }}
                val
              ></umb-property>
            `
          )}
        </div>
      </umb-property-layout>
    `;
  }

  render() {
    return html`
      <div class="settings">
        <umb-property-dataset
          .value=${this._values!}
          @change=${this.#onPropertyDataChange}
        >
          <umb-property
            alias="alias"
            label="Alias"
            description="The generated alias for this form. This is used for programmatic implementations."
            property-editor-ui-alias="Umb.PropertyEditorUi.TextBox"
            val
          ></umb-property>

          ${this.#renderFormType()}

          <umb-property-layout
            label="Submit outcome"
            description="What should happen to the client after they submit the form?"
          >
            <div slot="editor" class="option-container">
              ${repeat(
                this.#getAvailableOutcomes(),
                (item) => item.alias,
                (item) => html`
                  <div class="option">
                    <div class="option-header">
                      <h3>${item.displayName}</h3>
                      <uui-radio
                        .value=${item.alias}
                        .checked=${this.form?.definition.outcome.typeAlias ===
                        item.alias}
                        @change=${this.#onOutcomeChange}
                      ></uui-radio>
                    </div>
                    <div>
                      ${repeat(
                        item.properties,
                        (prop) => prop.alias,
                        (prop) => html`
                          <umb-property
                            alias=${"outcome-" + prop.alias}
                            label=${prop.displayName}
                            description=""
                            .readonly=${this.form?.definition.outcome
                              .typeAlias !== item.alias}
                            property-editor-ui-alias=${prop.propertyEditor}
                            .appearance=${{
                              labelOnTop: true,
                            }}
                            val
                          ></umb-property>
                        `
                      )}
                    </div>
                  </div>
                `
              )}
            </div>
          </umb-property-layout>
        </umb-property-dataset>
      </div>
    `;
  }

  static styles = css`
    .settings {
      padding: 0 16px;
      background-color: white;
    }

    .option-container {
      display: flex;
      flex-wrap: wrap;
      gap: 16px;
    }

    .option {
      padding: 16px 24px;
      border: 1px solid #ccc;
      border-radius: 8px;

      .option-header {
        display: flex;
        align-items: center;
        justify-content: space-between;
        margin-bottom: 8px;

        h3 {
          margin: 0;
        }
      }

      > h3 {
        margin: 0 0 8px;
      }

      .option-description {
        margin: 0 0 8px;
        color: var(--uui-color-text-alt);
      }
    }
  `;
}
