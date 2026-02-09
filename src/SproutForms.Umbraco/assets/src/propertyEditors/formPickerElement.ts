import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";
import {
  LitElement,
  html,
  customElement,
  property,
  when,
  state,
  css,
} from "@umbraco-cms/backoffice/external/lit";
import { UMB_MODAL_MANAGER_CONTEXT } from "@umbraco-cms/backoffice/modal";
import type { UmbPropertyEditorUiElement } from "@umbraco-cms/backoffice/property-editor";
import {
  UMB_TREE_PICKER_MODAL_ALIAS,
  UmbTreePickerModalValue,
} from "@umbraco-cms/backoffice/tree";
import { SproutFormsSource } from "../repositories/sproutFormsSource";
import { UmbChangeEvent } from "@umbraco-cms/backoffice/event";

@customElement("sf-form-picker")
export default class FormPickerElement
  extends UmbElementMixin(LitElement)
  implements UmbPropertyEditorUiElement
{
  #source = new SproutFormsSource(this);

  @property({ type: String })
  public set value(value: string) {
    this._value = value;
    this.getFormName();
  }
  public get value() {
    return this._value;
  }
  private _value: string = '';

  @state()
  public formName = "";

  #handleAddButton() {
    this.consumeContext(UMB_MODAL_MANAGER_CONTEXT, async (instance) => {
      const modal = instance!.open(this, UMB_TREE_PICKER_MODAL_ALIAS, {
        modal: { type: "sidebar", size: "small" },
        data: {
          treeAlias: "SproutFormTree",
          modalData: {
            entityType: "sf-form",
            preset: {},
          },
          /*search: {
			providerAlias: UMB_DOCUMENT_TYPE_SEARCH_PROVIDER_ALIAS,
		},*/
        },
      });
      await modal.onSubmit();

      const result = modal.getValue() as UmbTreePickerModalValue;
      if (result.selection[0]) {
        this.value = result.selection[0];
      } else {
        this.clearValues();
      }

      this.dispatchEvent(new UmbChangeEvent());
    });
  }

  clearValues() {
    this.value = "";
    this.formName = "";

    this.dispatchEvent(new UmbChangeEvent());
  }

  getFormName() {
    if (!this.value || this.value == ''){
      this.formName = '';
      return;
    }

    this.#source.getForm(this.value).then((resp) => {
      this.formName = resp.data.name;
    });
  }

  override render() {
    return when(
      !this.value || this.value === "",
      () => html`
        <uui-button
          id="btn-add"
          look="placeholder"
          label="Choose"
          type="button"
          color="default"
          @click=${this.#handleAddButton}
        ></uui-button>
      `,
      () =>
        html` <div class="edit-container">
          <uui-button
            id="btn-add"
            look="outline"
            .label="${this.formName}"
            type="button"
            color="default"
            @click=${this.#handleAddButton}
          ></uui-button>
          <uui-icon
            name="icon-trash"
            @click=${this.clearValues}
            class="delete-button"
          ></uui-icon>
        </div>`,
    );
  }

  static styles = css`
    uui-button {
      width: 100%;
    }

    .edit-container {
      display: flex;
      gap: 8px;
      align-items: center;
    }

    .delete-button {
      cursor: pointer;

      &:hover {
        color: red;
      }
    }
  `;
}

declare global {
  interface HTMLElementTagNameMap {
    "sf-form-picker": FormPickerElement;
  }
}
