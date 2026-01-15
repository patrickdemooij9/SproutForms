import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";
import {
  customElement,
  repeat,
  state,
  when,
} from "@umbraco-cms/backoffice/external/lit";
import { css, html, LitElement } from "lit";
import { FormBackofficeModel, FormFlowTypeBackofficeModel } from "../api";
import { SproutFormsSource } from "../repositories/sproutFormsSource";
import {
  UmbPropertyDatasetElement,
  UmbPropertyValueData,
} from "@umbraco-cms/backoffice/property";
import SproutFormsWorkspaceContext, {
  SF_FORM_DETAIL_TOKEN_CONTEXT,
} from "./sproutFormsWorkspaceContext";

@customElement("form-integrations")
export class FormIntegrationsElement extends UmbElementMixin(LitElement) {
  private context?: SproutFormsWorkspaceContext;

  @state()
  private form!: FormBackofficeModel;

  @state()
  private flowTypes: FormFlowTypeBackofficeModel[] = [];

  @state()
  values: { [key: string]: Array<UmbPropertyValueData> } = {};

  @state()
  private showAddContainer: boolean = false;

  @state()
  private openFlowType?: string = undefined;

  constructor() {
    super();

    new SproutFormsSource(this).getWorkflowTypes().then((resp) => {
      this.flowTypes = resp.data;
    });

    this.consumeContext(SF_FORM_DETAIL_TOKEN_CONTEXT, (context) => {
      this.context = context;

      context?.form.subscribe((form) => {
        this.form = form;

        this.values = {};
        this.form.definition.workflows.forEach((flow) => {
          this.values[flow.alias] = Object.entries(flow.configuration).map(
            ([key, value]) => ({
              alias: key,
              value: value,
            })
          );
        });
      });
    });
  }

  addFlowType(type: FormFlowTypeBackofficeModel) {
    let order = 0;
    if (this.form.definition.workflows.length > 0) {
      order = Math.max(
        ...this.form.definition.workflows.map((item) => item.order)
      );
    }

    const clonedDefinition = structuredClone(this.form.definition);
    const configuration: Record<string, string> = {};
    type.configuration.forEach((prop) => {
      configuration[prop.alias] = prop.value ?? "";
    });
    clonedDefinition.workflows.push({
      alias: "test",
      typeAlias: type.alias,
      displayName: type.displayName,
      order: order + 1,
      configuration: configuration,
    });
    this.showAddContainer = false;
    this.context?.updateForm({
      definition: clonedDefinition,
    });
  }

  getFlowType(alias: string) {
    return this.flowTypes.find((item) => item.alias == alias);
  }

  #onPropertyDataChange(e: Event, alias: string) {
    const value = (e.target as UmbPropertyDatasetElement).value;

    if (!this.values[alias]) {
      return;
    }

    const clonedDefinition = structuredClone(this.form.definition);
    const workflow = clonedDefinition.workflows.find(
      (item) => item.alias == alias
    );
    if (!workflow) {
      return;
    }
    value.forEach((item) => {
      if (Object.keys(workflow.configuration).includes(item.alias)) {
        workflow.configuration[item.alias] = item.value as string;
      }
    });
    this.context?.updateForm({
      definition: clonedDefinition,
    });
  }

  render() {
    return html`
      <div class="integrations">
        <h3>What happens after the user submits their form:</h3>
        ${repeat(
          this.form.definition.workflows.sort((a, b) => a.order - b.order),
          (item) => item.alias,
          (item) => html`
            <div class="add" @click=${() => (this.openFlowType = item.alias)}>
              ${item.displayName}
            </div>
            ${when(
              this.openFlowType == item.alias,
              () => html`
                <div class="add-container">
                  <umb-property-dataset
                    .value=${this.values[item.alias]}
                    @change=${(e: Event) =>
                      this.#onPropertyDataChange(e, item.alias)}
                  >
                    ${repeat(
                      this.getFlowType(item.typeAlias)?.configuration ?? [],
                      (prop) => prop.alias,
                      (prop) => html`
                        <umb-property
                          alias=${prop.alias}
                          label=${prop.displayName}
                          description=""
                          property-editor-ui-alias=${prop.propertyEditor}
                          .appearance=${{
                            labelOnTop: true,
                          }}
                          val
                        ></umb-property>
                      `
                    )}</umb-property-dataset
                  >
                </div>
              `
            )}
          `
        )}
        <div
          class="add"
          @click=${() => (this.showAddContainer = !this.showAddContainer)}
        >
          Add flow step
        </div>
        ${when(
          this.showAddContainer,
          () => html`
            <div class="add-container">
              <h3>Choose an flow type to add to your flow</h3>
              <div class="flow-types">
                ${repeat(
                  this.flowTypes,
                  (item) => item.alias,
                  (item) => html`
                    <div
                      class="flow-type"
                      @click=${() => this.addFlowType(item)}
                    >
                      <p>${item.displayName}</p>
                    </div>
                  `
                )}
              </div>
            </div>
          `
        )}
      </div>
    `;
  }

  static styles = css`
    .integrations {
      padding: 0 16px;
      background-color: white;

      position: relative;

      h3 {
        margin: 0;
        padding-top: 12px;
        padding-bottom: 12px;
      }
    }

    .add {
      display: flex;
      justify-content: center;
      align-items: center;
      padding: 8px 12px;
      border: 1px dashed #ccc;
      cursor: pointer;
    }

    .add-container {
      padding: 8px 12px;
      background-color: #e5e7eb;
      border: 1px solid #ccc;
    }

    .flow-types {
      display: grid;
      grid-template-columns: auto auto auto auto;

      .flow-type {
        padding: 2px 4px;
        border: 1px solid #ccc;
        border-radius: 8px;
        background-color: white;
        cursor: pointer;

        &:hover {
          background-color: #ccc;
        }
      }
    }
  `;
}
