import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";
import {
  customElement,
  html,
  LitElement,
  css,
  state,
} from "@umbraco-cms/backoffice/external/lit";
import { FormFlowTypeDto } from "../models";
import { SproutFormsSource } from "../repositories/sproutFormsSource";
import { WorkflowTemplateBackofficeModel } from "../api";

@customElement("sf-templates")
export class SfTemplatesElement extends UmbElementMixin(LitElement) {
  @state()
  private templates: WorkflowTemplateBackofficeModel[] = [];

  @state()
  private flowTypes: FormFlowTypeDto[] = [];

  @state()
  private isEditing = false;

  @state()
  private editingTemplate: WorkflowTemplateBackofficeModel | null = null;

  @state()
  private formName = "";

  @state()
  private formWorkflowTypeAlias = "";

  @state()
  private formConfiguration: Record<string, unknown> = {};

  @state()
  private formLockedFields: string[] = [];

  constructor() {
    super();
    this.loadTemplates();
    this.loadFlowTypes();
  }

  async loadTemplates() {
    const source = new SproutFormsSource(this);
    const resp = await source.getTemplates();
    if (resp.data) {
      this.templates = resp.data;
    }
  }

  async loadFlowTypes() {
    const source = new SproutFormsSource(this);
    const resp = await source.getWorkflowTypes();
    if (resp.data) {
      this.flowTypes = resp.data;
    }
  }

  openCreateModal() {
    this.isEditing = true;
    this.editingTemplate = null;
    this.formName = "";
    this.formWorkflowTypeAlias = this.flowTypes[0]?.alias || "";
    this.formConfiguration = {};
    this.formLockedFields = [];
    this.updateDefaultConfiguration();
  }

  openEditModal(template: WorkflowTemplateBackofficeModel) {
    //TODO: Move towards modal umbraco structure
    this.isEditing = true;
    this.editingTemplate = template;
    this.formName = template.name;
    this.formWorkflowTypeAlias = template.workflowTypeAlias;
    this.formConfiguration = template.configuration || {};
    this.formLockedFields = template.lockedFields || [];
  }

  closeModal() {
    this.isEditing = false;
    this.editingTemplate = null;
  }

  updateDefaultConfiguration() {
    this.formConfiguration = {};
    const flowType = this.flowTypes.find(
      (f) => f.alias === this.formWorkflowTypeAlias
    );
    if (flowType) {
      flowType.configuration.forEach((prop) => {
        if (!(prop.alias in this.formConfiguration)) {
          this.formConfiguration[prop.alias] = (prop.value as string) || "";
        }
      });
    }
  }

  workflowTypeChanged(e: Event) {
    const select = e.target as HTMLSelectElement;
    this.formWorkflowTypeAlias = select.value;
    this.updateDefaultConfiguration();
  }

  configFieldChanged(alias: string, value: string) {
    this.formConfiguration[alias] = value;
  }

  lockedFieldToggled(alias: string, checked: boolean) {
    if (checked) {
      if (!this.formLockedFields.includes(alias)) {
        this.formLockedFields = [...this.formLockedFields, alias];
      }
    } else {
      this.formLockedFields = this.formLockedFields.filter(
        (f) => f !== alias
      );
    }
  }

  async saveTemplate() {
    const source = new SproutFormsSource(this);
    const template: WorkflowTemplateBackofficeModel = {
      id: this.editingTemplate?.id,
      name: this.formName,
      workflowTypeAlias: this.formWorkflowTypeAlias,
      configuration: this.formConfiguration,
      lockedFields: this.formLockedFields,
    };

    if (this.editingTemplate) {
      await source.updateTemplate(template);
    } else {
      await source.createTemplate(template);
    }

    await this.loadTemplates();
    this.closeModal();
  }

  async deleteTemplate(id: string) {
    if (!confirm("Are you sure you want to delete this template?")) return;
    const source = new SproutFormsSource(this);
    await source.deleteTemplate(id);
    await this.loadTemplates();
  }

  getFlowTypeDisplayName(alias: string) {
    return this.flowTypes.find((f) => f.alias === alias)?.displayName || alias;
  }

  render() {
    return html`
      <div class="templates-header">
        <h3>Workflow Templates</h3>
        <uui-button
          look="primary"
          color="positive"
          @click=${this.openCreateModal}
        >
          Create Template
        </uui-button>
      </div>

      <div class="templates-list">
        ${this.templates.length === 0
          ? html`<p>No templates yet. Create one to get started.</p>`
          : this.templates.map(
              (template) => html`
                <div class="template-item">
                  <div class="template-info">
                    <strong>${template.name}</strong>
                    <span class="template-type"
                      >${this.getFlowTypeDisplayName(
                        template.workflowTypeAlias
                      )}</span
                    >
                    <span class="template-locked"
                      >${template.lockedFields?.length || 0} locked
                      field(s)</span
                    >
                  </div>
                  <div class="template-actions">
                    <uui-button
                      look="secondary"
                      @click=${() => this.openEditModal(template)}
                    >
                      Edit
                    </uui-button>
                    <uui-button
                      look="secondary"
                      color="danger"
                      @click=${() => this.deleteTemplate(template.id!)}
                    >
                      Delete
                    </uui-button>
                  </div>
                </div>
              `
            )}
      </div>

      ${this.isEditing ? this.renderModal() : ""}
    `;
  }

  renderModal() {
    const flowType = this.flowTypes.find(
      (f) => f.alias === this.formWorkflowTypeAlias
    );

    return html`
      <div class="modal-overlay" @click=${this.closeModal}>
        <div class="modal-content" @click=${(e: Event) => e.stopPropagation()}>
          <div class="modal-header">
            <h3>
              ${this.editingTemplate ? "Edit Template" : "Create Template"}
            </h3>
            <uui-button look="secondary" @click=${this.closeModal}
              >X</uui-button
            >
          </div>

          <div class="modal-body">
            <div class="form-group">
              <label>Template Name</label>
              <input
                type="text"
                .value=${this.formName}
                @input=${(e: Event) =>
                  (this.formName = (e.target as HTMLInputElement).value)}
              />
            </div>

            <div class="form-group">
              <label>Workflow Type</label>
              <select
                .value=${this.formWorkflowTypeAlias}
                .disabled=${this.editingTemplate !== null}
                @change=${this.workflowTypeChanged}
              >
                ${this.flowTypes.map(
                  (ft) =>
                    html`<option value=${ft.alias} .selected=${ft.alias === this.formWorkflowTypeAlias}>
                      ${ft.displayName}
                    </option>`
                )}
              </select>
            </div>

            ${flowType
              ? html`
                  <div class="form-group">
                    <label>Configuration</label>
                    ${flowType.configuration.map(
                      (prop) => html`
                        <div class="config-field">
                          <label>${prop.displayName}</label>
                          ${prop.propertyEditor.includes("TextArea")
                            ? html`
                                <textarea
                                  .value=${this.formConfiguration[prop.alias] as string || ""}
                                  @input=${(e: Event) =>
                                    this.configFieldChanged(
                                      prop.alias,
                                      (e.target as HTMLTextAreaElement).value
                                    )}
                                ></textarea>
                              `
                            : html`
                                <input
                                  type="text"
                                  .value=${this.formConfiguration[prop.alias] as string || ""}
                                  @input=${(e: Event) =>
                                    this.configFieldChanged(
                                      prop.alias,
                                      (e.target as HTMLInputElement).value
                                    )}
                                />
                              `}
                        </div>
                      `
                    )}
                  </div>

                  <div class="form-group">
                    <label>Locked Fields</label>
                    <p class="help-text">
                      Check fields that should be locked (cannot be changed when
                      using this template)
                    </p>
                    ${flowType.configuration.map(
                      (prop) => html`
                        <label class="checkbox-label">
                          <input
                            type="checkbox"
                            .checked=${this.formLockedFields.includes(prop.alias)}
                            @change=${(e: Event) =>
                              this.lockedFieldToggled(
                                prop.alias,
                                (e.target as HTMLInputElement).checked
                              )}
                          />
                          ${prop.displayName}
                        </label>
                      `
                    )}
                  </div>
                `
              : ""}
          </div>

          <div class="modal-footer">
            <uui-button look="secondary" @click=${this.closeModal}>
              Cancel
            </uui-button>
            <uui-button
              look="primary"
              color="positive"
              @click=${this.saveTemplate}
            >
              Save
            </uui-button>
          </div>
        </div>
      </div>
    `;
  }

  static styles = css`
    .templates-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 20px;
    }

    .templates-header h3 {
      margin: 0;
    }

    .templates-list {
      display: flex;
      flex-direction: column;
      gap: 10px;
    }

    .template-item {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 15px;
      background: var(--uui-color-surface);
      border: 1px solid var(--uui-color-border);
      border-radius: 4px;
    }

    .template-info {
      display: flex;
      flex-direction: column;
      gap: 5px;
    }

    .template-type {
      color: var(--uui-color-text-alt);
      font-size: 0.9em;
    }

    .template-locked {
      color: var(--uui-color-text-alt);
      font-size: 0.85em;
    }

    .template-actions {
      display: flex;
      gap: 10px;
    }

    .modal-overlay {
      position: fixed;
      top: 0;
      left: 0;
      right: 0;
      bottom: 0;
      background: rgba(0, 0, 0, 0.5);
      display: flex;
      justify-content: center;
      align-items: center;
      z-index: 1000;
    }

    .modal-content {
      background: var(--uui-color-surface);
      border-radius: 8px;
      width: 600px;
      max-height: 80vh;
      overflow-y: auto;
    }

    .modal-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 20px;
      border-bottom: 1px solid var(--uui-color-border);
    }

    .modal-header h3 {
      margin: 0;
    }

    .modal-body {
      padding: 20px;
    }

    .modal-footer {
      display: flex;
      justify-content: flex-end;
      gap: 10px;
      padding: 20px;
      border-top: 1px solid var(--uui-color-border);
    }

    .form-group {
      margin-bottom: 20px;
    }

    .form-group label {
      display: block;
      margin-bottom: 5px;
      font-weight: 600;
    }

    .form-group input[type="text"],
    .form-group select,
    .form-group textarea {
      width: 100%;
      padding: 8px;
      border: 1px solid var(--uui-color-border);
      border-radius: 4px;
    }

    .form-group textarea {
      min-height: 80px;
    }

    .config-field {
      margin-bottom: 10px;
    }

    .config-field label {
      font-size: 0.9em;
    }

    .checkbox-label {
      display: flex;
      align-items: center;
      gap: 8px;
      margin-bottom: 5px;
    }

    .help-text {
      font-size: 0.85em;
      color: var(--uui-color-text-alt);
      margin-bottom: 10px;
    }
  `;
}

export default SfTemplatesElement;
