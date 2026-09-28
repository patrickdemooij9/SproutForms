import { css, customElement, html } from "@umbraco-cms/backoffice/external/lit";
import { UmbWorkspaceElement } from "@umbraco-cms/backoffice/workspace";
import { RECYCLE_BIN_COLLECTION_ALIAS } from "./recycleBinCollectionContext";

@customElement("sf-recycle-bin-workspace")
export default class RecycleBinWorkspaceElement extends UmbWorkspaceElement {
  render() {
    return html`
      <umb-body-layout>
        <div slot="header" class="header">
          <uui-button
            compact
            href="/umbraco/section/sproutForms"
            label="Back to the forms"
          >
            <uui-icon name="icon-arrow-left"></uui-icon>
          </uui-button>
          <umb-icon name="icon-trash"></umb-icon>
          <h3>Recycle bin</h3>
        </div>
        <p class="intro">
          Deleted forms stay here with their submissions until they are restored or deleted permanently.
        </p>
        <umb-collection alias=${RECYCLE_BIN_COLLECTION_ALIAS}></umb-collection>
      </umb-body-layout>
    `;
  }

  static styles = css`
    .header {
      display: flex;
      align-items: center;
      gap: var(--uui-size-space-3);
    }

    h3 {
      margin: 0;
    }

    .intro {
      margin-top: 0;
      color: var(--uui-color-text-alt);
    }
  `;
}

declare global {
  interface HTMLElementTagNameMap {
    "sf-recycle-bin-workspace": RecycleBinWorkspaceElement;
  }
}
