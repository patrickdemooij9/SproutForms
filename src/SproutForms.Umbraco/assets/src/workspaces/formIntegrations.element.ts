import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";
import {
  customElement,
  repeat,
  state,
  when,
} from "@umbraco-cms/backoffice/external/lit";
import { css, html, LitElement } from "lit";
import { SproutFormsSource } from "../repositories/sproutFormsSource";
import SproutFormsWorkspaceContext, {
  SF_FORM_DETAIL_TOKEN_CONTEXT,
} from "./sproutFormsWorkspaceContext";
import { FormDto, FormFlowTypeDto, FormWorkflowDto } from "../models";
import { FlowChangeEvent } from "./formIntegrationTypeInspector.element";

import "./formIntegrationTypeInspector.element";

@customElement("form-integrations")
export class FormIntegrationsElement extends UmbElementMixin(LitElement) {
  private context?: SproutFormsWorkspaceContext;

  @state()
  private form!: FormDto;

  @state()
  private flowTypes: FormFlowTypeDto[] = [];

  @state()
  private selectedFlow?: FormWorkflowDto = undefined;

  constructor() {
    super();

    new SproutFormsSource(this).getWorkflowTypes().then((resp) => {
      this.flowTypes = resp.data;
    });

    this.consumeContext(SF_FORM_DETAIL_TOKEN_CONTEXT, (context) => {
      this.context = context;

      context?.form.subscribe((form) => {
        this.form = form;
      });
    });
  }

  addFlowType(type: FormFlowTypeDto) {
    let order = 0;
    if (this.form.definition.workflows.length > 0) {
      order = Math.max(
        ...this.form.definition.workflows.map((item) => item.order),
      );
    }

    const clonedDefinition = structuredClone(this.form.definition);
    const configuration: Record<string, string> = {};
    type.configuration.forEach((prop) => {
      configuration[prop.alias] = (prop.value as string) ?? "";
    });
    const newFlow = {
      id: crypto.randomUUID(),
      alias: crypto.randomUUID(),
      typeAlias: type.alias,
      displayName: type.displayName,
      order: order + 1,
      configuration: configuration,
    };
    clonedDefinition.workflows.push(newFlow);
    this.selectedFlow = newFlow;
    this.context?.updateForm({
      definition: clonedDefinition,
    });
  }

  getOrderedFlow() {
    return [...this.form.definition.workflows].sort((a, b) => {
      return a.order - b.order;
    });
  }

  getFlowType(alias: string) {
    return this.flowTypes.find((item) => item.alias == alias);
  }

  #handleFlowUpdated(e: FlowChangeEvent) {
    const updatedWorkflow = e.flow;
    const clonedDefinition = structuredClone(this.form.definition);
    const workflow = clonedDefinition.workflows.find(
      (item) => item.id == updatedWorkflow.id,
    );
    if (!workflow) {
      return;
    }

    Object.assign(workflow, updatedWorkflow);
    this.context?.updateForm({
      definition: clonedDefinition,
    });
  }

  render() {
    return html`
      <div class="integrations">
        <div class="editor-container">
          <h3>Submission workflow</h3>
          <p>
            Define the steps that should happen in the background after the
            forms has been submitted
          </p>
          <div class="editor">
            <div class="block start">
              Form has been submitted (visitor finishes)
            </div>
            <div class="arrow">
              <uui-icon name="icon-arrow-down"></uui-icon>
            </div>
            ${repeat(
              this.getOrderedFlow(),
              (item) => item.alias,
              (item) => html`
                <div
                  class="flow-type ${this.selectedFlow?.alias === item.alias
                    ? "selected"
                    : ""}"
                  @click=${() => (this.selectedFlow = item)}
                >
                  ${item.displayName}
                </div>
              `,
            )}
            <div
              class="flow-type add"
              @click=${() => (this.selectedFlow = undefined)}
            >
              Add flow step
            </div>
          </div>
        </div>
        <div class="inspector">
          ${when(
            this.selectedFlow !== undefined,
            () => html`
              <sf-integration-type-inspector
                .flow=${this.selectedFlow!}
                .flowType=${this.getFlowType(this.selectedFlow!.typeAlias)!}
                @flow-change=${this.#handleFlowUpdated}
              ></sf-integration-type-inspector>
            `,
            () => html`
              <div class="content">
                <h3>Choose your flow to add</h3>
                ${repeat(
                  this.flowTypes,
                  (flowType) => flowType.alias,
                  (flowType) =>
                    html` <button @click=${() => this.addFlowType(flowType)}>
                      ${flowType.displayName}
                    </button>`,
                )}
              </div>
            `,
          )}
        </div>
      </div>
    `;
  }

  static styles = css`
    .integrations {
      padding: 0 16px;
      background-color: white;
      height: 100%;

      position: relative;

      display: grid;
      grid-template-columns: 2fr 1fr;
      gap: 24px;
      height: 100%;
    }

    .editor-container {
      padding-top: 12px;

      h3 {
        margin: 0;
        padding-top: 12px;
      }
    }

    .editor {
      display: flex;
      flex-direction: column;
      gap: 8px;

      .block {
        display: flex;
        align-items: center;
        justify-content: center;

        padding: 12px;
        border: 1px solid #ccc;
        border-radius: 8px;

        &.start {
          background-color: #7bf1a8;
        }
      }

      .arrow {
        display: flex;
        align-items: center;
        justify-content: center;
      }
    }

    .inspector {
      border-left: 1px solid #ccc;

      .content {
        padding: 18px 12px;
      }

      button {
        display: block;
        width: 100%;
        margin-bottom: 8px;
        border: 1px solid #ccc;
        border-radius: 4px;
        background-color: transparent;
        padding: 8px 12px;
        cursor: pointer;

        &:hover {
          background-color: #e5e7eb;
        }
      }

      umb-property-layout {
        padding: 0;
      }
    }

    .flow-type {
      display: flex;
      justify-content: center;
      align-items: center;
      padding: 8px 12px;
      border: 1px solid #ccc;
      cursor: pointer;

      &.add {
        border: 1px dashed #ccc;
      }

      &:hover,
      &.selected {
        background-color: #e5e7eb;
      }
    }
  `;
}
