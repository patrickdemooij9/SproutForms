import { UmbWorkspaceElement } from "@umbraco-cms/backoffice/workspace";
import {
  css,
  customElement,
  html,
  state,
  when,
} from "@umbraco-cms/backoffice/external/lit";

import "./formEditor.element";
import "./formSettings.element";
import "./formIntegrations.element";
import "./formSubmissions.element";
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
}

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

  render() {
    return html`
      <div class="workspace">
        <div class="header">
          <uui-input
            placeholder="Name of the form"
            .value=${this.form.name}
            @change=${(event: UUIInputEvent) =>
              this.updateName(event.target.value as string)}
            .readonly=${this.form.source == SOURCE_CODE}
          ></uui-input>
          <div class="navigation">
            <div
              class="${this.tabState == TabState.Editor ? "selected" : ""}"
              @click=${() => (this.tabState = TabState.Editor)}
            >
              Build
            </div>
            <div
              class="${this.tabState == TabState.Settings ? "selected" : ""}"
              @click=${() => (this.tabState = TabState.Settings)}
            >
              Settings
            </div>
            <div
              class="${this.tabState == TabState.Integrations
                ? "selected"
                : ""}"
              @click=${() => (this.tabState = TabState.Integrations)}
            >
              Integrations
            </div>
            ${when(
              this.form.id,
              () => html`
                <div
                  class="${this.tabState == TabState.Submissions
                    ? "selected"
                    : ""}"
                  @click=${() => (this.tabState = TabState.Submissions)}
                >
                  Submissions
                </div>
              `
            )}
          </div>
          <div></div>
        </div>
        <div class="content">
          ${when(
          this.tabState == TabState.Editor,
          () => html`<form-editor></form-editor>`
        )}
        ${when(
          this.tabState == TabState.Settings,
          () => html`<form-settings></form-settings>`
        )}
        ${when(
          this.tabState == TabState.Integrations,
          () => html`<form-integrations></form-integrations>`
        )}
        ${when(
          this.tabState == TabState.Submissions,
          () => html`<form-submissions></form-submissions>`
        )}
        ${when(
          this.form.source === SOURCE_CODE && this.tabState !== TabState.Submissions,
          () => html`
            <div class="overlay">
              <p>Code forms cannot be edited in the backoffice.</p>
            </div>
          `
        )}
        </div>
        <div class="footer">
          <div>Version: ${this.form.version}</div>
          <div>
            <uui-button
              id="save"
              label="Submit"
              look="primary"
              color="positive"
              @click=${() => this.save()}
              .disabled=${this.form.source === SOURCE_CODE}
            >
              Save
            </uui-button>
          </div>
        </div>
      </div>
    `;
  }

  static styles = css`
    .workspace {
      height: 100%;
      display: flex;
      flex-direction: column;
    }

    .content,
    form-editor,
    form-settings,
    form-integrations,
    form-submissions {
      flex-grow: 1;
      overflow-y: auto;
      background-color: white;
    }

    .header {
      display: flex;
      justify-content: space-between;
      flex-shrink: 0;
      align-items: center;
      width: 100%;
      background-color: white;
      border-bottom: 1px solid #ccc;
      height: 54px;
      padding: 0 16px;

      > * {
        flex: 1;
      }
    }

    .navigation {
      display: flex;
      justify-content: center;
      gap: 16px;
      cursor: pointer;

      .selected {
        font-weight: 700;
        border-bottom: 1px solid #ccc;
      }
    }

    .content {
      position: relative;
    }

    .footer {
      display: flex;
      flex-shrink: 0;
      justify-content: space-between;
      align-items: center;
      background-color: white;
      border-top: 1px solid #ccc;
      height: 54px;
      padding: 0 16px;
    }

    .overlay {
      position: absolute;
      top: 0;
      width: 100%;
      height: 100%;
      background-color: rgba(0, 0, 0, 0.3);

      display: flex;
      justify-content: center;
      align-items: center;

      p {
        padding: 16px 24px;
        background-color: white;
        border-radius: 4px;
      }
    }
  `;
}
