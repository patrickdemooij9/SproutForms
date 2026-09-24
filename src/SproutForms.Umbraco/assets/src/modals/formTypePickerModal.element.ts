import {
  css,
  customElement,
  html,
  repeat,
} from "@umbraco-cms/backoffice/external/lit";
import { UmbModalBaseElement } from "@umbraco-cms/backoffice/modal";
import {
  FormDefinitionTypeDto,
  FormTypePickerModalData,
  FormTypePickerModalValue,
} from "../models";

@customElement("sf-form-type-picker-modal")
export default class FormTypePickerModalElement extends UmbModalBaseElement<
  FormTypePickerModalData,
  FormTypePickerModalValue
> {
  #pick(formType: FormDefinitionTypeDto) {
    this.updateValue({ formTypeAlias: formType.alias });
    this._submitModal();
  }

  override render() {
    return html`
      <umb-body-layout headline="Create a form">
        <p class="intro">
          What kind of form do you want to create? The type can't be changed
          later.
        </p>
        <div class="types">
          ${repeat(
            this.data?.formTypes ?? [],
            (formType) => formType.alias,
            (formType) => html`
              <button class="type" @click=${() => this.#pick(formType)}>
                <strong>${formType.displayName}</strong>
                <span>${formType.description}</span>
              </button>
            `
          )}
        </div>
        <uui-button
          slot="actions"
          label="Cancel"
          @click=${() => this._rejectModal()}
        ></uui-button>
      </umb-body-layout>
    `;
  }

  static override styles = css`
    .intro {
      margin-top: 0;
    }

    .types {
      display: flex;
      flex-direction: column;
      gap: 8px;
    }

    .type {
      display: flex;
      flex-direction: column;
      gap: 4px;
      padding: 12px 16px;
      border: 1px solid var(--uui-color-border);
      border-radius: var(--uui-border-radius);
      background-color: var(--uui-color-surface);
      color: var(--uui-color-text);
      font: inherit;
      text-align: left;
      cursor: pointer;

      &:hover,
      &:focus-visible {
        border-color: var(--uui-color-interactive-emphasis);
        background-color: var(--uui-color-surface-emphasis);
      }

      span {
        color: var(--uui-color-text-alt);
      }
    }
  `;
}

declare global {
  interface HTMLElementTagNameMap {
    "sf-form-type-picker-modal": FormTypePickerModalElement;
  }
}
