import { UmbWorkspaceElement } from "@umbraco-cms/backoffice/workspace";
import {
  css,
  customElement,
  html,
  nothing,
  repeat,
  state,
  when,
} from "@umbraco-cms/backoffice/external/lit";

import "./formEditor.element";
import "./formSettings.element";
import "./formIntegrations.element";
import "./formSubmissions.element";
import "./formInfo.element";
import { UUIInputEvent } from "@umbraco-cms/backoffice/external/uui";
import SproutFormsWorkspaceContext, {
  SF_FORM_DETAIL_TOKEN_CONTEXT,
} from "./sproutFormsWorkspaceContext";
import { FormDto, SOURCE_CODE } from "../models";

enum TabState {
  Editor,
  Settings,
  Integrations,
  Submissions,
  Info,
}

type Tab = { state: TabState; label: string; icon: string };

@customElement("sprout-forms-workspace")
export class SproutFormsWorkspaceElement extends UmbWorkspaceElement {
  private context?: SproutFormsWorkspaceContext;

  @state()
  private form!: FormDto;

  @state()
  private tabState: TabState = TabState.Editor;

  constructor() {
    super();

    this.consumeContext(SF_FORM_DETAIL_TOKEN_CONTEXT, (context) => {
      this.context = context;

      context?.form.subscribe((form) => {
        this.form = form;
      });
    });
  }

  updateName(name: string) {
    this.context?.setName(name);
  }

  async save() {
    await this.context?.save();
  }

  // Submissions and history only exist once the form has been saved
  #getTabs(): Array<Tab> {
    const tabs: Array<Tab> = [
      { state: TabState.Editor, label: "Build", icon: "icon-layout" },
      { state: TabState.Settings, label: "Settings", icon: "icon-settings" },
      { state: TabState.Integrations, label: "Integrations", icon: "icon-nodes" },
    ];
    if (this.form.id) {
      tabs.push({ state: TabState.Submissions, label: "Submissions", icon: "icon-inbox" });
      tabs.push({ state: TabState.Info, label: "Info", icon: "icon-info" });
    }
    return tabs;
  }

  get #isReadOnly() {
    return this.form.source === SOURCE_CODE && this.tabState !== TabState.Submissions && this.tabState !== TabState.Info;
  }

  render() {
    return html`
      <umb-body-layout main-no-padding>
        <div slot="header" class="header">
          <uui-input
            id="nameInput"
            label="Name of the form"
            placeholder="Name of the form"
            .value=${this.form.name}
            @change=${(event: UUIInputEvent) =>
              this.updateName(event.target.value as string)}
            .readonly=${this.form.source == SOURCE_CODE}
          ></uui-input>
          ${this.form.definition.type.typeAlias !== "standard"
            ? html`<uui-tag look="secondary">${this.form.definition.type.displayName}</uui-tag>`
            : nothing}
        </div>

        <uui-tab-group slot="navigation">
          ${repeat(
            this.#getTabs(),
            (tab) => tab.state,
            (tab) => html`
              <uui-tab
                .label=${tab.label}
                ?active=${this.tabState === tab.state}
                @click=${() => (this.tabState = tab.state)}
              >
                <umb-icon slot="icon" name=${tab.icon}></umb-icon>
                ${tab.label}
              </uui-tab>
            `,
          )}
        </uui-tab-group>

        <div class="content">
          ${when(
            this.#isReadOnly,
            () => html`
              <div class="read-only-notice">
                <umb-icon name="icon-lock"></umb-icon>
                This form is defined in code, so it can't be edited in the backoffice.
              </div>
            `,
          )}
          <div class="view" ?inert=${this.#isReadOnly}>
            ${when(
              this.tabState == TabState.Editor,
              () => html`<form-editor></form-editor>`,
            )}
            ${when(
              this.tabState == TabState.Settings,
              () => html`<form-settings></form-settings>`,
            )}
            ${when(
              this.tabState == TabState.Integrations,
              () => html`<form-integrations></form-integrations>`,
            )}
            ${when(
              this.tabState == TabState.Submissions,
              () => html`<form-submissions></form-submissions>`,
            )}
            ${when(
              this.tabState == TabState.Info,
              () => html`<form-info></form-info>`,
            )}
          </div>
        </div>

        <div slot="footer-info" class="footer-info">
          ${when(
            this.form.source === SOURCE_CODE,
            () => html`<uui-tag look="outline">Code</uui-tag>`,
          )}
          <span>Version ${this.form.version}</span>
        </div>
        <uui-button
          slot="actions"
          id="save"
          label="Save"
          look="primary"
          color="positive"
          @click=${() => this.save()}
          .disabled=${this.form.source === SOURCE_CODE}
        ></uui-button>
      </umb-body-layout>
    `;
  }

  static styles = css`
    :host {
      display: block;
      height: 100%;
    }

    .header {
      display: flex;
      align-items: center;
      gap: var(--uui-size-space-4);
      width: 100%;
    }

    #nameInput {
      flex: 1 1 auto;
    }

    uui-tab-group {
      --uui-tab-divider: var(--uui-color-border);
      border-left: 1px solid var(--uui-color-border);
      border-right: 1px solid var(--uui-color-border);
    }

    .content {
      display: flex;
      flex-direction: column;
      height: 100%;
    }

    .view {
      flex: 1;
      min-height: 0;
    }

    .view[inert] {
      opacity: 0.75;
    }

    form-editor,
    form-settings,
    form-integrations,
    form-submissions,
    form-info {
      display: block;
      height: 100%;
    }

    .read-only-notice {
      display: flex;
      align-items: center;
      gap: var(--uui-size-space-3);
      flex-shrink: 0;
      padding: var(--uui-size-space-3) var(--uui-size-layout-1);
      background-color: var(--uui-color-warning);
      color: var(--uui-color-warning-contrast);
      border-bottom: 1px solid var(--uui-color-warning-standalone);
    }

    .footer-info {
      display: flex;
      align-items: center;
      gap: var(--uui-size-space-3);
      padding-left: var(--uui-size-layout-1);
      color: var(--uui-color-text-alt);
    }
  `;
}
