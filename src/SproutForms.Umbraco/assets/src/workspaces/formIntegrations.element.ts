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
import { FormFlowTypeBackofficeModel } from "../api";

@customElement("form-integrations")
export class FormIntegrationsElement extends UmbElementMixin(LitElement) {
  private context?: SproutFormsWorkspaceContext;

  @state()
  private form!: FormDto;

  @state()
  private flowTypes: FormFlowTypeBackofficeModel[] = [];

  @state()
  private selectedFlowId?: string = undefined;

  @state()
  private selectedFlow?: FormWorkflowDto = undefined;

  @state()
  private draggedWorkflowId?: string = undefined;

  constructor() {
    super();

    new SproutFormsSource(this).getWorkflowTypes().then((resp) => {
      this.flowTypes = resp.data;
    });

    this.consumeContext(SF_FORM_DETAIL_TOKEN_CONTEXT, (context) => {
      this.context = context;

      context?.form.subscribe((form) => {
        this.form = form;
        this.selectedFlow = this.selectedFlowId ? this.form.definition.workflows.find((item) => item.id === this.selectedFlowId) : undefined;
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
    this.selectedFlowId = newFlow.id;
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

  getFlowTypeConfiguration(alias: string) {
    return this.flowTypes.find((item) => item.alias == alias)?.configuration ?? [];
  }

  formatWorkflowTitle(workflow: FormWorkflowDto): string {
    const flowType = this.getFlowType(workflow.typeAlias);
    if (!flowType) {
      return workflow.displayName;
    }

    let title = flowType.displayTemplate;
    const configProps = flowType.configuration;

    for (const prop of configProps) {
      const token = `{${prop.alias}}`;
      if (title.includes(token)) {
        const value = workflow.configuration[prop.alias];
        title = title.replace(token, value ? String(value) : "");
      }
    }

    return title;
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

  private deleteWorkflow(event: MouseEvent, workflowId: string) {
    event.stopPropagation();
    if (this.selectedFlowId === workflowId) {
      this.selectedFlowId = undefined;
      this.selectedFlow = undefined;
    }
    this.context?.removeWorkflow(workflowId);
  }

  private onDragStartWorkflow(event: DragEvent, workflowId: string) {
    this.draggedWorkflowId = workflowId;
    if (event.dataTransfer) {
      event.dataTransfer.effectAllowed = 'move';
      event.dataTransfer.setData('text/plain', workflowId);
    }
  }

  private onDragOverWorkflow(event: DragEvent) {
    event.preventDefault();
    if (event.dataTransfer) {
      event.dataTransfer.dropEffect = 'move';
    }
  }

  private onDropWorkflow(event: DragEvent, targetWorkflowId: string) {
    event.preventDefault();
    event.stopPropagation();
    
    if (!this.draggedWorkflowId || this.draggedWorkflowId === targetWorkflowId) {
      this.draggedWorkflowId = undefined;
      return;
    }

    const orderedFlow = this.getOrderedFlow();
    const draggedIndex = orderedFlow.findIndex(w => w.id === this.draggedWorkflowId);
    const targetIndex = orderedFlow.findIndex(w => w.id === targetWorkflowId);

    if (draggedIndex === -1 || targetIndex === -1) {
      this.draggedWorkflowId = undefined;
      return;
    }

    const newOrder = [...orderedFlow];
    const [draggedItem] = newOrder.splice(draggedIndex, 1);
    newOrder.splice(targetIndex, 0, draggedItem);

    this.context?.reorderWorkflows(newOrder.map(w => w.id));
    this.draggedWorkflowId = undefined;
  }

  private onDragEnd() {
    this.draggedWorkflowId = undefined;
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
                  class="flow-type ${this.selectedFlow?.id === item.id
                    ? "selected"
                    : ""} ${this.draggedWorkflowId && this.draggedWorkflowId !== item.id ? 'drag-over' : ''}"
                  draggable="true"
                  @dragstart=${(event: DragEvent) => this.onDragStartWorkflow(event, item.id)}
                  @dragover=${this.onDragOverWorkflow}
                  @drop=${(event: DragEvent) => this.onDropWorkflow(event, item.id)}
                  @dragend=${this.onDragEnd}
                  @click=${() => {
                    this.selectedFlowId = item.id;
                    this.selectedFlow = this.form.definition.workflows.find((item) => item.id === this.selectedFlowId);
                  }}
                >
                  <span class="workflow-name">${this.formatWorkflowTitle(item)}</span>
                  <button
                    class="delete-btn"
                    @click=${(event: MouseEvent) => this.deleteWorkflow(event, item.id)}
                    title="Delete workflow"
                  >
                    <uui-icon name="icon-delete"></uui-icon>
                  </button>
                </div>
              `,
            )}
            <div
              class="flow-type add"
              @click=${() => {
                this.selectedFlowId = undefined;
                this.selectedFlow = undefined;
              }}
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
                    html` <div class="flow-type-option">
                      <button @click=${() => this.addFlowType(flowType)}>
                        ${flowType.displayName}
                      </button>
                    </div>`,
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
      background-color: #f3f4f6;
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
      background-color: white;
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

      .flow-type-option {
        margin-bottom: 8px;
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
      border-radius: 4px;
      cursor: pointer;
      background-color: white;

      &.add {
        border: 1px dashed #ccc;
      }

      &:hover,
      &.selected {
        background-color: #e5e7eb;
      }

      &.drag-over {
        border: 2px dashed #0078d4;
        background-color: #e6f2fa;
      }

      .workflow-name {
        flex: 1;
      }

      .delete-btn {
        opacity: 0;
        background: none;
        border: none;
        cursor: pointer;
        padding: 4px;
        display: flex;
        align-items: center;
        justify-content: center;
        color: #666;
        transition: opacity 0.2s, color 0.2s;

        &:hover {
          color: #d32f2f;
        }
      }

      &:hover .delete-btn {
        opacity: 1;
      }
    }
  `;
}
