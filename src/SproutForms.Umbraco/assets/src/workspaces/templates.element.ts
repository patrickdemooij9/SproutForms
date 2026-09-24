import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";
import {
  customElement,
  html,
  LitElement,
  css,
  repeat,
  state,
  when,
} from "@umbraco-cms/backoffice/external/lit";
import { umbConfirmModal, umbOpenModal } from "@umbraco-cms/backoffice/modal";
import { SproutFormsSource } from "../repositories/sproutFormsSource";
import { FormFlowTypeBackofficeModel, WorkflowTemplateBackofficeModel } from "../api";
import { WORKFLOW_TEMPLATE_MODAL } from "../modals/workflowTemplateModal.token";

@customElement("sf-templates")
export class SfTemplatesElement extends UmbElementMixin(LitElement) {
  #source = new SproutFormsSource(this);

  @state()
  private templates: WorkflowTemplateBackofficeModel[] = [];

  @state()
  private flowTypes: FormFlowTypeBackofficeModel[] = [];

  @state()
  private loaded = false;

  constructor() {
    super();
    this.loadTemplates();
    this.#source.getWorkflowTypes().then((resp) => {
      this.flowTypes = resp.data ?? [];
    });
  }

  async loadTemplates() {
    const resp = await this.#source.getTemplates();
    if (resp.data) {
      this.templates = resp.data;
    }
    this.loaded = true;
  }

  async #openTemplate(template?: WorkflowTemplateBackofficeModel) {
    try {
      await umbOpenModal(this, WORKFLOW_TEMPLATE_MODAL, {
        data: { template },
      });
    } catch {
      // Cancelled
      return;
    }
    await this.loadTemplates();
  }

  async #deleteTemplate(template: WorkflowTemplateBackofficeModel) {
    try {
      await umbConfirmModal(this, {
        headline: "Delete template",
        content: html`Delete <strong>${template.name}</strong>? Workflow steps
          that use it keep their settings, but its locked settings no longer
          apply.`,
        color: "danger",
        confirmLabel: "Delete",
      });
    } catch {
      // Cancelled
      return;
    }
    await this.#source.deleteTemplate(template.id!);
    await this.loadTemplates();
  }

  #getFlowType(alias: string) {
    return this.flowTypes.find((f) => f.alias === alias);
  }

  #getLockedFieldNames(template: WorkflowTemplateBackofficeModel) {
    const configuration = this.#getFlowType(template.workflowTypeAlias)?.configuration ?? [];
    return (template.lockedFields ?? []).map(
      (alias) => configuration.find((prop) => prop.alias === alias)?.displayName ?? alias,
    );
  }

  render() {
    return html`
      <div class="templates">
        <div class="header">
          <div>
            <h3>Workflow templates</h3>
            <p>
              Reusable workflow steps. Add one to a form from its Integrations
              tab, or save a form's step as a template there.
            </p>
          </div>
          <uui-button
            look="primary"
            color="positive"
            label="Create template"
            @click=${() => this.#openTemplate()}
          >
            <uui-icon name="icon-add"></uui-icon> Create template
          </uui-button>
        </div>

        ${when(
          this.loaded && this.templates.length === 0,
          () => html`
            <div class="empty">
              <uui-icon name="icon-zip"></uui-icon>
              <h4>No templates yet</h4>
              <p>
                A template holds the settings of a workflow step, such as who an
                email goes to, so you don't have to set them up on every form.
              </p>
            </div>
          `,
          () => html`
            <div class="list">
              ${repeat(
                this.templates,
                (template) => template.id,
                (template) => this.#renderTemplate(template),
              )}
            </div>
          `,
        )}
      </div>
    `;
  }

  #renderTemplate(template: WorkflowTemplateBackofficeModel) {
    const lockedNames = this.#getLockedFieldNames(template);

    return html`
      <div class="template">
        <button class="template-main" @click=${() => this.#openTemplate(template)}>
          <span class="template-icon"><uui-icon name="icon-zip"></uui-icon></span>
          <span class="template-text">
            <span class="template-name">${template.name}</span>
            <span class="template-type">
              ${this.#getFlowType(template.workflowTypeAlias)?.displayName ?? template.workflowTypeAlias}
            </span>
          </span>
        </button>
        <div class="template-locks">
          ${lockedNames.length > 0
            ? repeat(
                lockedNames,
                (name) => name,
                (name) => html`
                  <uui-tag look="secondary" title="Locked">
                    <uui-icon name="icon-lock"></uui-icon> ${name}
                  </uui-tag>
                `,
              )
            : html`<span class="no-locks">Nothing locked</span>`}
        </div>
        <div class="template-actions">
          <uui-button
            compact
            label="Edit"
            title="Edit"
            @click=${() => this.#openTemplate(template)}
          >
            <uui-icon name="icon-edit"></uui-icon>
          </uui-button>
          <uui-button
            compact
            color="danger"
            label="Delete"
            title="Delete"
            @click=${() => this.#deleteTemplate(template)}
          >
            <uui-icon name="icon-trash"></uui-icon>
          </uui-button>
        </div>
      </div>
    `;
  }

  static styles = css`
    .templates {
      max-width: 1100px;
      margin: 0 auto;
      padding: var(--uui-size-layout-1);
    }

    .header {
      display: flex;
      justify-content: space-between;
      align-items: flex-start;
      gap: var(--uui-size-layout-1);
      margin-bottom: var(--uui-size-layout-1);

      h3 {
        margin: 0 0 var(--uui-size-space-2);
      }

      p {
        margin: 0;
        color: var(--uui-color-text-alt);
      }

      uui-button {
        flex-shrink: 0;
      }
    }

    .list {
      display: flex;
      flex-direction: column;
      background: var(--uui-color-surface);
      border: 1px solid var(--uui-color-border);
      border-radius: calc(var(--uui-border-radius) * 2);
      box-shadow: var(--uui-shadow-depth-1);
      overflow: hidden;
    }

    .template {
      display: grid;
      grid-template-columns: minmax(200px, 1fr) minmax(0, 1.5fr) auto;
      align-items: center;
      gap: var(--uui-size-space-5);
      padding: var(--uui-size-space-3) var(--uui-size-space-5);

      &:not(:last-child) {
        border-bottom: 1px solid var(--uui-color-divider-standalone);
      }

      &:hover {
        background: var(--uui-color-surface-emphasis);
      }
    }

    .template-main {
      display: flex;
      align-items: center;
      gap: var(--uui-size-space-4);
      min-width: 0;
      padding: 0;
      font: inherit;
      color: inherit;
      text-align: left;
      background: none;
      border: none;
      cursor: pointer;

      &:hover .template-name {
        color: var(--uui-color-interactive-emphasis);
        text-decoration: underline;
      }
    }

    .template-icon {
      display: flex;
      align-items: center;
      justify-content: center;
      flex-shrink: 0;
      width: 36px;
      height: 36px;
      border-radius: var(--uui-border-radius);
      background: var(--uui-color-surface-alt);
      color: var(--uui-color-interactive);
    }

    .template-text {
      display: flex;
      flex-direction: column;
      min-width: 0;
    }

    .template-name,
    .template-type {
      overflow: hidden;
      text-overflow: ellipsis;
      white-space: nowrap;
    }

    .template-name {
      font-weight: 700;
    }

    .template-type {
      font-size: var(--uui-type-small-size);
      color: var(--uui-color-text-alt);
    }

    .template-locks {
      display: flex;
      flex-wrap: wrap;
      gap: var(--uui-size-space-2);

      uui-icon {
        margin-right: var(--uui-size-space-1);
      }
    }

    .no-locks {
      font-size: var(--uui-type-small-size);
      color: var(--uui-color-text-alt);
    }

    .template-actions {
      display: flex;
      gap: var(--uui-size-space-1);
    }

    .empty {
      display: flex;
      flex-direction: column;
      align-items: center;
      padding: var(--uui-size-layout-3) var(--uui-size-layout-1);
      text-align: center;
      background: var(--uui-color-surface);
      border: 1px dashed var(--uui-color-border-emphasis);
      border-radius: calc(var(--uui-border-radius) * 2);

      uui-icon {
        font-size: 2em;
        color: var(--uui-color-text-alt);
      }

      h4 {
        margin: var(--uui-size-space-4) 0 var(--uui-size-space-2);
      }

      p {
        max-width: 420px;
        margin: 0;
        color: var(--uui-color-text-alt);
      }
    }
  `;
}

export default SfTemplatesElement;
