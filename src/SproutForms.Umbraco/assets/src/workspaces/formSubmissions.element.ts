import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";
import {
  css,
  customElement,
  html,
  LitElement,
  state,
} from "@umbraco-cms/backoffice/external/lit";

import {
  SUBMISSIONS_COLLECTION_ALIAS,
  SUBMISSIONS_RECYCLE_BIN_COLLECTION_ALIAS,
} from "../collections/submissionCollectionAliases";

// The form's recycle bin is next to its submissions, so its entries show with the same columns
@customElement("form-submissions")
export class FormSubmissionsElement extends UmbElementMixin(LitElement) {
  @state()
  private _showRecycleBin = false;

  #renderSwitch(label: string, icon: string, showRecycleBin: boolean) {
    return html`
      <uui-button
        look=${this._showRecycleBin === showRecycleBin ? "primary" : "secondary"}
        label=${label}
        @click=${() => (this._showRecycleBin = showRecycleBin)}
      >
        <umb-icon name=${icon}></umb-icon> ${label}
      </uui-button>
    `;
  }

  render() {
    return html`
      <uui-button-group id="switch">
        ${this.#renderSwitch("Submissions", "icon-inbox", false)}
        ${this.#renderSwitch("Recycle bin", "icon-trash", true)}
      </uui-button-group>
      ${this._showRecycleBin
        ? html`<umb-collection alias=${SUBMISSIONS_RECYCLE_BIN_COLLECTION_ALIAS}></umb-collection>`
        : html`<umb-collection alias=${SUBMISSIONS_COLLECTION_ALIAS}></umb-collection>`}
    `;
  }

  static styles = css`
    :host {
      display: flex;
      flex-direction: column;
    }

    #switch {
      margin: var(--uui-size-layout-1) var(--uui-size-layout-1) 0;
      align-self: flex-start;
    }

    umb-collection {
      flex: 1;
    }
  `;
}
