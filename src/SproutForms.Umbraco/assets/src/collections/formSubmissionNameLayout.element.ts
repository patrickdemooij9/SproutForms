import {
  customElement,
  property,
  css,
  html,
  nothing,
} from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UMB_MODAL_MANAGER_CONTEXT } from "@umbraco-cms/backoffice/modal";

@customElement("sf-form-submission-name-column-layout")
export class FormSubmissionNameLayout extends UmbLitElement {
  @property({ attribute: false })
  value?: {
    unique: string;
    name: string;
    callback: (event: Event, unique: string) => void;
  };

  #handleClick() {
    this.consumeContext(UMB_MODAL_MANAGER_CONTEXT, async (instance) => {
      const modal = instance!.open(this, "sproutForms.modal.submission.info", {
        modal: { type: "sidebar", size: "medium" },
        data: {
          submissionId: this.value?.unique,
        },
      });
      await modal.onSubmit();
    });
  }

  override render() {
    if (!this.value) return nothing;

    return html`<a @click="${this.#handleClick}">${this.value.name}</a>`;
  }

  static override styles = [
    css`
      :host {
        white-space: nowrap;
      }

      a {
        color: var(--uui-color-interactive);
      }

      a:hover {
        color: var(--uui-color-interactive-emphasis);
      }
    `,
  ];
}

declare global {
  interface HTMLElementTagNameMap {
    "sf-form-submission-name-column-layout": FormSubmissionNameLayout;
  }
}
