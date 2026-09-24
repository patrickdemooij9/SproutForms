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
import { FormFlowTypeBackofficeModel, WorkflowTemplateBackofficeModel } from "../api";
import { umbOpenModal } from "@umbraco-cms/backoffice/modal";
import { WORKFLOW_TEMPLATE_MODAL } from "../modals/workflowTemplateModal.token";

@customElement("form-integrations")
export class FormIntegrationsElement extends UmbElementMixin(LitElement) {
  private context?: SproutFormsWorkspaceContext;

  @state()
  private form!: FormDto;

  @state()
  private flowTypes: FormFlowTypeBackofficeModel[] = [];

  @state()
  private templates: WorkflowTemplateBackofficeModel[] = [];

  @state()
  private selectedFlowId?: string = undefined;

  @state()
  private selectedFlow?: FormWorkflowDto = undefined;

  @state()
  private draggedWorkflowId?: string = undefined;

  @state()
  private showTemplateSelectorFor: string | null = null;

  constructor() {
    super();

    const source = new SproutFormsSource(this);
    source.getWorkflowTypes().then((resp) => {
      this.flowTypes = resp.data;
    });
    this.#loadTemplates();

    this.consumeContext(SF_FORM_DETAIL_TOKEN_CONTEXT, (context) => {
      this.context = context;

      context?.form.subscribe((form) => {
        this.form = form;
        this.selectedFlow = this.selectedFlowId ? this.form.definition.workflows.find((item) => item.id === this.selectedFlowId) : undefined;
      });
    });
  }

  async #loadTemplates() {
    const resp = await new SproutFormsSource(this).getTemplates();
    this.templates = resp.data ?? [];
  }

  // Saves the step as a new template, or edits the template it uses, and links the step to it
  async #openTemplateForFlow(workflow: FormWorkflowDto) {
    const existing = this.getTemplateForFlow(workflow.templateId);
    let saved: WorkflowTemplateBackofficeModel;
    try {
      const value = await umbOpenModal(this, WORKFLOW_TEMPLATE_MODAL, {
        data: {
          template: existing ?? {
            name: this.getFlowType(workflow.typeAlias)?.displayName ?? workflow.displayName,
            workflowTypeAlias: workflow.typeAlias,
            configuration: structuredClone(workflow.configuration),
            lockedFields: [],
          },
        },
      });
      saved = value.template;
    } catch {
      // Cancelled
      return;
    }

    await this.#loadTemplates();
    if (workflow.templateId === saved.id) return;

    const clonedDefinition = structuredClone(this.form.definition);
    const clonedWorkflow = clonedDefinition.workflows.find((it) => it.id === workflow.id);
    if (!clonedWorkflow) return;
    clonedWorkflow.templateId = saved.id;
    this.context?.updateForm({ definition: clonedDefinition });
  }

  getTemplatesForType(typeAlias: string) {
    return this.templates.filter(t => t.workflowTypeAlias === typeAlias);
  }

  getTemplateForFlow(templateId: string | null | undefined) {
    if (!templateId) return null;
    return this.templates.find(t => t.id === templateId) || null;
  }

  addFlowType(type: FormFlowTypeDto, template?: WorkflowTemplateBackofficeModel) {
    let order = 0;
    if (this.form.definition.workflows.length > 0) {
      order = Math.max(
        ...this.form.definition.workflows.map((item) => item.order),
      );
    }

    const clonedDefinition = structuredClone(this.form.definition);
    let configuration: Record<string, string> = {};
    
    if (template) {
      Object.entries(template.configuration).forEach(([alias, value]) => {
        configuration[alias] = (value as string) ?? "";
      });
    } else {
      type.configuration.forEach((prop) => {
        configuration[prop.alias] = (prop.value as string) ?? "";
      });
    }
    
    const newFlow = {
      id: crypto.randomUUID(),
      alias: crypto.randomUUID(),
      typeAlias: type.alias,
      displayName: type.displayName,
      order: order + 1,
      configuration: configuration,
      templateId: template?.id || null,
    };
    clonedDefinition.workflows.push(newFlow);
    this.selectedFlowId = newFlow.id;
    this.showTemplateSelectorFor = null;
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

  #selectFlow(workflowId: string | undefined) {
    this.selectedFlowId = workflowId;
    this.selectedFlow = workflowId
      ? this.form.definition.workflows.find((item) => item.id === workflowId)
      : undefined;
  }

  render() {
    return html`
      <div class="integrations">
        <div class="editor-container">
          <div class="intro">
            <h3>Submission workflow</h3>
            <p>
              The steps that run in the background after the form has been
              submitted, in this order. Drag a step to reorder it.
            </p>
          </div>
          <ol class="timeline">
            <li class="step start">
              <span class="marker"><uui-icon name="icon-check"></uui-icon></span>
              <div class="card">
                <span class="card-title">Form submitted</span>
                <span class="card-subtitle">The visitor has finished the form</span>
              </div>
            </li>
            ${repeat(
              this.getOrderedFlow(),
              (item) => item.alias,
              (item, index) => html`
                <li
                  class="step ${this.draggedWorkflowId && this.draggedWorkflowId !== item.id ? "drag-over" : ""} ${this.draggedWorkflowId === item.id ? "dragging" : ""}"
                  draggable="true"
                  @dragstart=${(event: DragEvent) => this.onDragStartWorkflow(event, item.id)}
                  @dragover=${this.onDragOverWorkflow}
                  @drop=${(event: DragEvent) => this.onDropWorkflow(event, item.id)}
                  @dragend=${this.onDragEnd}
                >
                  <span class="marker">${index + 1}</span>
                  <div
                    class="card workflow ${this.selectedFlow?.id === item.id ? "selected" : ""}"
                    @click=${() => this.#selectFlow(item.id)}
                  >
                    <uui-icon class="grip" name="icon-grip"></uui-icon>
                    <div class="card-text">
                      <span class="card-title">${this.formatWorkflowTitle(item)}</span>
                      <span class="card-subtitle">${this.#getFlowSubtitle(item)}</span>
                    </div>
                    <button
                      class="delete-btn"
                      @click=${(event: MouseEvent) => this.deleteWorkflow(event, item.id)}
                      title="Delete workflow"
                      aria-label="Delete workflow"
                    >
                      <uui-icon name="icon-trash"></uui-icon>
                    </button>
                  </div>
                </li>
              `,
            )}
            <li class="step">
              <span class="marker add"><uui-icon name="icon-add"></uui-icon></span>
              <button
                class="card add ${this.selectedFlow ? "" : "selected"}"
                @click=${() => this.#selectFlow(undefined)}
              >
                Add a step
              </button>
            </li>
          </ol>
        </div>
        <div class="inspector">
          ${when(
            this.selectedFlow !== undefined,
            () => html`
              <div class="panel-header">
                <div class="panel-title">
                  <h3>${this.formatWorkflowTitle(this.selectedFlow!)}</h3>
                  <span>${this.#getFlowSubtitle(this.selectedFlow!)}</span>
                </div>
                <uui-button
                  compact
                  look="outline"
                  label=${this.getTemplateForFlow(this.selectedFlow!.templateId) ? "Edit template" : "Save as template"}
                  title=${this.getTemplateForFlow(this.selectedFlow!.templateId)
                    ? "Edit the template this step uses. Changes apply to every form that uses it."
                    : "Save this step's settings as a template you can reuse on other forms"}
                  @click=${() => this.#openTemplateForFlow(this.selectedFlow!)}
                >
                  <uui-icon name="icon-zip"></uui-icon>
                  ${this.getTemplateForFlow(this.selectedFlow!.templateId) ? "Edit template" : "Save as template"}
                </uui-button>
                <uui-button
                  compact
                  label="Close"
                  title="Back to the step list"
                  @click=${() => this.#selectFlow(undefined)}
                >
                  <uui-icon name="icon-wrong"></uui-icon>
                </uui-button>
              </div>
              <sf-integration-type-inspector
                .flow=${this.selectedFlow!}
                .flowType=${this.getFlowType(this.selectedFlow!.typeAlias)!}
                .lockedFields=${this.getTemplateForFlow(this.selectedFlow!.templateId)?.lockedFields ?? []}
                @flow-change=${this.#handleFlowUpdated}
              ></sf-integration-type-inspector>
            `,
            () => html`
              <div class="panel-header">
                <div class="panel-title">
                  <h3>Add a step</h3>
                  <span>It runs after the steps that are already there</span>
                </div>
              </div>
              <div class="content">
                ${repeat(
                  this.flowTypes,
                  (flowType) => flowType.alias,
                  (flowType) => this.#renderFlowTypeOption(flowType),
                )}
              </div>
            `,
          )}
        </div>
      </div>
    `;
  }

  #getFlowSubtitle(workflow: FormWorkflowDto) {
    const typeName = this.getFlowType(workflow.typeAlias)?.displayName ?? workflow.displayName;
    const template = this.getTemplateForFlow(workflow.templateId);
    return template ? `${typeName} · Template: ${template.name}` : typeName;
  }

  #renderFlowTypeOption(flowType: FormFlowTypeBackofficeModel) {
    const templates = this.getTemplatesForType(flowType.alias);
    const isTemplateSelectorOpen = this.showTemplateSelectorFor === flowType.alias;

    return html`
      <div class="flow-type-option">
        <div class="option-row">
          <button class="option-add" @click=${() => this.addFlowType(flowType)}>
            <uui-icon name="icon-add"></uui-icon>
            ${flowType.displayName}
          </button>
          ${templates.length > 0
            ? html`
                <button
                  class="template-btn ${isTemplateSelectorOpen ? "active" : ""}"
                  title="Use template"
                  aria-label="Use template"
                  @click=${(e: Event) => {
                    e.stopPropagation();
                    this.showTemplateSelectorFor = isTemplateSelectorOpen ? null : flowType.alias;
                  }}
                >
                  <uui-icon name="icon-zip"></uui-icon>
                </button>
              `
            : ""}
        </div>
        ${isTemplateSelectorOpen
          ? html`
              <div class="template-selector">
                <p class="selector-label">Start from a template</p>
                ${templates.map(
                  (template) => html`
                    <button
                      class="template-option"
                      @click=${() => this.addFlowType(flowType, template)}
                    >
                      ${template.name}
                    </button>
                  `,
                )}
                <uui-button
                  compact
                  label="Cancel"
                  @click=${(e: Event) => {
                    e.stopPropagation();
                    this.showTemplateSelectorFor = null;
                  }}
                ></uui-button>
              </div>
            `
          : ""}
      </div>
    `;
  }

  static styles = css`
    :host {
      display: block;
    }

    .integrations {
      display: grid;
      grid-template-columns: minmax(0, 1fr) minmax(320px, 400px);
      height: 100%;
    }

    .editor-container {
      overflow-y: auto;
      background-color: var(--uui-color-background);
    }

    .intro,
    .timeline {
      max-width: 720px;
      margin: 0 auto;
      padding: 0 var(--uui-size-layout-1);
    }

    .intro {
      padding-top: var(--uui-size-layout-1);

      h3 {
        margin: 0 0 var(--uui-size-space-2);
      }

      p {
        margin: 0 0 var(--uui-size-space-5);
        color: var(--uui-color-text-alt);
      }
    }

    .timeline {
      list-style: none;
      padding-bottom: var(--uui-size-layout-1);
    }

    .step {
      position: relative;
      display: flex;
      align-items: stretch;
      gap: var(--uui-size-space-4);
      padding-bottom: var(--uui-size-space-4);

      /* The line that connects the step markers */
      &:not(:last-child)::before {
        content: "";
        position: absolute;
        left: 13px;
        top: 28px;
        bottom: 0;
        width: 2px;
        background: var(--uui-color-border);
      }

      &.dragging {
        opacity: 0.5;
      }

      &.drag-over .card {
        border-style: dashed;
        border-color: var(--uui-color-interactive-emphasis);
      }
    }

    .marker {
      display: flex;
      align-items: center;
      justify-content: center;
      flex-shrink: 0;
      width: 28px;
      height: 28px;
      margin-top: 12px;
      box-sizing: border-box;
      border-radius: 50%;
      background: var(--uui-color-surface);
      border: 2px solid var(--uui-color-border-emphasis);
      font-size: var(--uui-type-small-size);
      font-weight: 700;
      z-index: 1;

      &.add {
        border-style: dashed;
        color: var(--uui-color-text-alt);
      }
    }

    .start .marker {
      background: var(--uui-color-positive);
      border-color: var(--uui-color-positive);
      color: var(--uui-color-positive-contrast);
    }

    .card {
      display: flex;
      align-items: center;
      gap: var(--uui-size-space-3);
      flex: 1;
      min-width: 0;
      min-height: 52px;
      box-sizing: border-box;
      padding: var(--uui-size-space-3) var(--uui-size-space-4);
      background: var(--uui-color-surface);
      border: 1px solid var(--uui-color-border);
      border-radius: calc(var(--uui-border-radius) * 2);
      font: inherit;
      color: var(--uui-color-text);
      text-align: left;
    }

    .start .card {
      flex-direction: column;
      align-items: flex-start;
      justify-content: center;
      gap: 0;
    }

    .card.workflow {
      padding-left: var(--uui-size-space-2);
      box-shadow: var(--uui-shadow-depth-1);
      cursor: pointer;
      transition: border-color 120ms, box-shadow 120ms;

      &:hover {
        border-color: var(--uui-color-border-emphasis);
      }
    }

    .card.selected {
      border-color: var(--uui-color-selected);
      box-shadow: 0 0 0 1px var(--uui-color-selected);
    }

    .card.add {
      justify-content: center;
      background: transparent;
      border-style: dashed;
      border-color: var(--uui-color-border-emphasis);
      color: var(--uui-color-text-alt);
      cursor: pointer;

      &:hover,
      &.selected {
        color: var(--uui-color-interactive-emphasis);
        border-color: var(--uui-color-interactive-emphasis);
        background: var(--uui-color-surface);
        box-shadow: none;
      }

      &.selected {
        border-style: solid;
      }
    }

    .grip {
      flex-shrink: 0;
      color: var(--uui-color-disabled-contrast);
      cursor: grab;
    }

    .card-text {
      display: flex;
      flex-direction: column;
      flex: 1;
      min-width: 0;
    }

    .card-title,
    .card-subtitle {
      overflow: hidden;
      text-overflow: ellipsis;
      white-space: nowrap;
    }

    .card-title {
      font-weight: 700;
    }

    .card-subtitle {
      font-size: var(--uui-type-small-size);
      color: var(--uui-color-text-alt);
    }

    .delete-btn {
      display: flex;
      align-items: center;
      justify-content: center;
      flex-shrink: 0;
      padding: var(--uui-size-space-2);
      background: none;
      border: none;
      border-radius: var(--uui-border-radius);
      color: var(--uui-color-text-alt);
      cursor: pointer;
      opacity: 0;
      transition: opacity 120ms, color 120ms, background-color 120ms;

      &:hover {
        color: var(--uui-color-danger);
        background-color: var(--uui-color-surface-emphasis);
      }

      &:focus-visible {
        opacity: 1;
      }
    }

    .card.workflow:hover .delete-btn,
    .card.workflow.selected .delete-btn {
      opacity: 1;
    }

    .inspector {
      overflow-y: auto;
      background-color: var(--uui-color-surface);
      border-left: 1px solid var(--uui-color-border);

      .content {
        padding: var(--uui-size-space-5);
      }

      umb-property-layout {
        padding: 0;
      }
    }

    .panel-header {
      display: flex;
      align-items: center;
      gap: var(--uui-size-space-3);
      padding: var(--uui-size-space-4) var(--uui-size-space-5);
      border-bottom: 1px solid var(--uui-color-border);
    }

    .panel-header uui-button uui-icon {
      margin-right: var(--uui-size-space-1);
    }

    .panel-title {
      display: flex;
      flex-direction: column;
      flex: 1;
      min-width: 0;

      h3 {
        margin: 0;
        overflow: hidden;
        text-overflow: ellipsis;
        white-space: nowrap;
      }

      span {
        font-size: var(--uui-type-small-size);
        color: var(--uui-color-text-alt);
      }
    }

    .flow-type-option {
      margin-bottom: var(--uui-size-space-3);
    }

    .option-row {
      display: flex;
      gap: var(--uui-size-space-2);
    }

    .option-add,
    .template-btn,
    .template-option {
      display: flex;
      align-items: center;
      gap: var(--uui-size-space-3);
      padding: var(--uui-size-space-3) var(--uui-size-space-4);
      font: inherit;
      color: var(--uui-color-text);
      text-align: left;
      background-color: var(--uui-color-surface);
      border: 1px solid var(--uui-color-border);
      border-radius: calc(var(--uui-border-radius) * 2);
      cursor: pointer;
      transition: border-color 120ms, color 120ms;

      &:hover,
      &.active {
        border-color: var(--uui-color-interactive-emphasis);
        color: var(--uui-color-interactive-emphasis);
      }
    }

    .option-add {
      flex: 1;

      uui-icon {
        color: var(--uui-color-interactive);
      }
    }

    .template-btn {
      justify-content: center;
      width: 44px;
      padding: 0;
    }

    .template-selector {
      display: flex;
      flex-direction: column;
      gap: var(--uui-size-space-2);
      margin-top: var(--uui-size-space-2);
      padding: var(--uui-size-space-3);
      background-color: var(--uui-color-background);
      border-radius: calc(var(--uui-border-radius) * 2);

      uui-button {
        align-self: flex-start;
      }
    }

    .selector-label {
      margin: 0;
      font-size: var(--uui-type-small-size);
      color: var(--uui-color-text-alt);
    }
  `;
}
