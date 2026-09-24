import {
  css,
  customElement,
  html,
  nothing,
  repeat,
  state,
} from "@umbraco-cms/backoffice/external/lit";
import { UmbModalBaseElement } from "@umbraco-cms/backoffice/modal";
import {
  UmbPropertyDatasetElement,
  UmbPropertyValueData,
} from "@umbraco-cms/backoffice/property";
import { UUIInputEvent, UUISelectEvent } from "@umbraco-cms/backoffice/external/uui";
import { FormFlowTypeBackofficeModel, WorkflowTemplateBackofficeModel } from "../api";
import { SproutFormsSource } from "../repositories/sproutFormsSource";
import {
  WorkflowTemplateModalData,
  WorkflowTemplateModalValue,
} from "./workflowTemplateModal.token";

@customElement("sf-workflow-template-modal")
export default class WorkflowTemplateModalElement extends UmbModalBaseElement<
  WorkflowTemplateModalData,
  WorkflowTemplateModalValue
> {
  #source = new SproutFormsSource(this);

  @state()
  private _flowTypes: Array<FormFlowTypeBackofficeModel> = [];

  @state()
  private _name = "";

  @state()
  private _workflowTypeAlias = "";

  @state()
  private _configuration: Record<string, unknown> = {};

  @state()
  private _lockedFields: Array<string> = [];

  @state()
  private _saving = false;

  get #isNew() {
    return !this.data?.template?.id;
  }

  get #flowType() {
    return this._flowTypes.find((it) => it.alias === this._workflowTypeAlias);
  }

  get #values(): Array<UmbPropertyValueData> {
    return Object.entries(this._configuration).map(([alias, value]) => ({ alias, value }));
  }

  override connectedCallback() {
    super.connectedCallback();

    const template = this.data?.template;
    this._name = template?.name ?? "";
    this._workflowTypeAlias = template?.workflowTypeAlias ?? "";
    this._configuration = { ...(template?.configuration ?? {}) };
    this._lockedFields = [...(template?.lockedFields ?? [])];

    this.#source.getWorkflowTypes().then((resp) => {
      this._flowTypes = resp.data ?? [];
      if (!this._workflowTypeAlias && this._flowTypes.length > 0) {
        this.#setWorkflowType(this._flowTypes[0].alias);
      }
    });
  }

  // A new workflow type starts from that type's default configuration
  #setWorkflowType(alias: string) {
    this._workflowTypeAlias = alias;
    const configuration: Record<string, unknown> = {};
    this.#flowType?.configuration.forEach((prop) => {
      configuration[prop.alias] = prop.value ?? "";
    });
    this._configuration = configuration;
    this._lockedFields = [];
  }

  #onConfigurationChange(e: Event) {
    const value = (e.target as UmbPropertyDatasetElement).value;
    const configuration = { ...this._configuration };
    value.forEach((item) => {
      configuration[item.alias] = item.value;
    });
    this._configuration = configuration;
  }

  #toggleLock(alias: string) {
    this._lockedFields = this._lockedFields.includes(alias)
      ? this._lockedFields.filter((it) => it !== alias)
      : [...this._lockedFields, alias];
  }

  async #save() {
    if (!this._name.trim() || !this._workflowTypeAlias) return;

    const template: WorkflowTemplateBackofficeModel = {
      id: this.data?.template?.id,
      name: this._name.trim(),
      workflowTypeAlias: this._workflowTypeAlias,
      configuration: this._configuration,
      lockedFields: this._lockedFields,
    };

    this._saving = true;
    const resp = this.#isNew
      ? await this.#source.createTemplate(template)
      : await this.#source.updateTemplate(template);
    this._saving = false;

    // A failed request already shows a notification
    if (!resp.data) return;

    this.updateValue({ template: resp.data });
    this._submitModal();
  }

  #renderWorkflowType() {
    // The type of an existing template, or of one made from a workflow step, is fixed
    const isFixed = !this.#isNew || !!this.data?.template?.workflowTypeAlias;
    if (isFixed) {
      return html`<span class="fixed-value">${this.#flowType?.displayName ?? this._workflowTypeAlias}</span>`;
    }

    return html`
      <uui-select
        label="Workflow type"
        .options=${this._flowTypes.map((it) => ({
          name: it.displayName,
          value: it.alias,
          selected: it.alias === this._workflowTypeAlias,
        }))}
        @change=${(e: UUISelectEvent) => this.#setWorkflowType(e.target.value as string)}
      ></uui-select>
    `;
  }

  #renderConfiguration() {
    const flowType = this.#flowType;
    if (!flowType || flowType.configuration.length === 0) return nothing;

    return html`
      <uui-box headline="Configuration">
        <p class="box-description">
          Lock a setting to keep it the same on every form that uses this
          template. Settings that aren't locked are only a starting point, and
          can be changed per form.
        </p>
        <umb-property-dataset
          .value=${this.#values}
          @change=${this.#onConfigurationChange}
        >
          ${repeat(
            flowType.configuration,
            (prop) => prop.alias,
            (prop) => {
              const isLocked = this._lockedFields.includes(prop.alias);
              return html`
                <div class="config-row ${isLocked ? "locked" : ""}">
                  <umb-property
                    alias=${prop.alias}
                    label=${prop.displayName}
                    property-editor-ui-alias=${prop.propertyEditor}
                    .appearance=${{ labelOnTop: true }}
                  ></umb-property>
                  <uui-button
                    class="lock-btn"
                    compact
                    look=${isLocked ? "primary" : "outline"}
                    label=${isLocked ? "Unlock" : "Lock"}
                    title=${isLocked
                      ? "Locked: forms can't change this setting"
                      : "Not locked: forms can change this setting"}
                    @click=${() => this.#toggleLock(prop.alias)}
                  >
                    <uui-icon name=${isLocked ? "icon-lock" : "icon-unlocked"}></uui-icon>
                    ${isLocked ? "Locked" : "Lock"}
                  </uui-button>
                </div>
              `;
            },
          )}
        </umb-property-dataset>
      </uui-box>
    `;
  }

  override render() {
    return html`
      <umb-body-layout headline=${this.#isNew ? "Create workflow template" : "Edit workflow template"}>
        <div class="content">
          <uui-box headline="General">
            <umb-property-layout label="Name" orientation="vertical" mandatory>
              <uui-input
                slot="editor"
                label="Name"
                placeholder="For example: Notify the sales team"
                .value=${this._name}
                @input=${(e: UUIInputEvent) => (this._name = e.target.value as string)}
              ></uui-input>
            </umb-property-layout>
            <umb-property-layout label="Workflow type" orientation="vertical">
              <div slot="editor">${this.#renderWorkflowType()}</div>
            </umb-property-layout>
          </uui-box>

          ${this.#renderConfiguration()}
        </div>

        <uui-button
          slot="actions"
          label="Cancel"
          @click=${() => this._rejectModal()}
        ></uui-button>
        <uui-button
          slot="actions"
          look="primary"
          color="positive"
          label=${this.#isNew ? "Create" : "Save"}
          .state=${this._saving ? "waiting" : undefined}
          ?disabled=${!this._name.trim() || !this._workflowTypeAlias}
          @click=${this.#save}
        ></uui-button>
      </umb-body-layout>
    `;
  }

  static override styles = css`
    .content {
      display: flex;
      flex-direction: column;
      gap: var(--uui-size-layout-1);
    }

    uui-input,
    uui-select {
      width: 100%;
    }

    umb-property-layout {
      padding-top: 0;
    }

    .fixed-value {
      font-weight: 700;
    }

    .box-description {
      margin: 0 0 var(--uui-size-space-4);
      color: var(--uui-color-text-alt);
    }

    .config-row {
      display: flex;
      align-items: flex-start;
      gap: var(--uui-size-space-3);
      margin: 0 calc(var(--uui-size-space-3) * -1);
      padding: 0 var(--uui-size-space-3);
      border-left: 3px solid transparent;
      border-radius: var(--uui-border-radius);
      transition: background-color 120ms, border-color 120ms;

      umb-property {
        flex: 1;
        min-width: 0;
      }

      &.locked {
        background-color: var(--uui-color-surface-alt);
        border-left-color: var(--uui-color-selected);
      }
    }

    .lock-btn {
      flex-shrink: 0;
      margin-top: var(--uui-size-space-5);

      uui-icon {
        margin-right: var(--uui-size-space-1);
      }
    }
  `;
}

declare global {
  interface HTMLElementTagNameMap {
    "sf-workflow-template-modal": WorkflowTemplateModalElement;
  }
}
