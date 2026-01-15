import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";
import {
  customElement,
  repeat,
  state,
} from "@umbraco-cms/backoffice/external/lit";
import { css, html, LitElement } from "lit";
import { FormBackofficeModel, FormOutcomeTypeBackofficeModel } from "../api";
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

@customElement("form-settings")
export class FormSettingsElement extends UmbElementMixin(LitElement) {
  private context?: SproutFormsWorkspaceContext;

  @state()
  form?: FormBackofficeModel;

  @state()
  _values: Array<UmbPropertyValueData> = [];

  @state()
  outcomes: Array<FormOutcomeTypeBackofficeModel> = [];

  constructor() {
    super();

    new SproutFormsSource(this).getOutcomes().then((resp) => {
      this.outcomes = resp.data;
    });

    this.consumeContext(SF_FORM_DETAIL_TOKEN_CONTEXT, (context) => {
      this.context = context;

      context?.form.subscribe((form) => {
        this.form = form;

        this._values = [
          {
            alias: "alias",
            value: this.form.alias,
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

    const updateForm: Partial<FormBackofficeModel> = {};
    updateForm.definition = structuredClone(this.form!.definition);
    value.forEach((item) => {
      if (item.alias == "alias") {
        updateForm.alias = item.value as string;
        this.context?.lockAliasUpdate();
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
      configuration[prop.alias] = prop.value ?? "";
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

          <umb-property-layout
            label="Submit outcome"
            description="What should happen to the client after they submit the form?"
          >
            <div slot="editor" class="outcome-container">
              ${repeat(
                this.outcomes,
                (item) => item.alias,
                (item) => html`
                  <div class="outcome">
                    <div class="outcome-header">
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

    .outcome-container {
      display: flex;
      gap: 16px;
    }

    .outcome {
      padding: 16px 24px;
      border: 1px solid #ccc;
      border-radius: 8px;

      .outcome-header {
        display: flex;
        align-items: center;
        justify-content: space-between;
        margin-bottom: 8px;

        h3 {
          margin: 0;
        }
      }
    }
  `;
}
