import { css, customElement, html, nothing, state } from "@umbraco-cms/backoffice/external/lit";
import { keyed } from "lit/directives/keyed.js";
import { UmbWorkspaceElement } from "@umbraco-cms/backoffice/workspace";
import { SF_FORM_SUBMISSIONS_TOKEN_CONTEXT } from "./formSubmissionsWorkspaceContext";

import "./formSubmissions.element";

@customElement("sf-form-submissions-workspace")
export default class FormSubmissionsWorkspaceElement extends UmbWorkspaceElement {
  @state()
  private _formId?: string;

  @state()
  private _formName?: string;

  constructor() {
    super();

    this.consumeContext(SF_FORM_SUBMISSIONS_TOKEN_CONTEXT, (context) => {
      this.observe(context?.formId, (formId) => (this._formId = formId));
      this.observe(context?.formName, (formName) => (this._formName = formName));
    });
  }

  // Another form gets a new collection, so it doesn't show the submissions loaded before
  render() {
    return html`
      <umb-body-layout main-no-padding>
        <div slot="header" class="header">
          <uui-button
            compact
            href="/umbraco/section/sproutForms"
            label="Back to the forms"
          >
            <uui-icon name="icon-arrow-left"></uui-icon>
          </uui-button>
          <umb-icon name="icon-inbox"></umb-icon>
          <h3>${this._formName ?? ""} <span class="subtitle">Submissions</span></h3>
          ${this._formId
            ? html`
                <uui-button
                  class="edit"
                  look="secondary"
                  label="Edit form"
                  href="/umbraco/section/sproutForms/workspace/sprout-form/edit/${this._formId}"
                >
                  <uui-icon name="icon-edit"></uui-icon> Edit form
                </uui-button>
              `
            : nothing}
        </div>
        ${this._formId ? keyed(this._formId, html`<form-submissions></form-submissions>`) : nothing}
      </umb-body-layout>
    `;
  }

  static styles = css`
    .header {
      display: flex;
      align-items: center;
      gap: var(--uui-size-space-3);
      width: 100%;
    }

    h3 {
      margin: 0;
    }

    .subtitle {
      margin-left: var(--uui-size-space-2);
      font-weight: normal;
      color: var(--uui-color-text-alt);
    }

    .edit {
      margin-left: auto;
    }
  `;
}

declare global {
  interface HTMLElementTagNameMap {
    "sf-form-submissions-workspace": FormSubmissionsWorkspaceElement;
  }
}
