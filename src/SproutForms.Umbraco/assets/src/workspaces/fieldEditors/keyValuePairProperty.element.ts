import { IFormFieldConfigElement } from "../../manifests/formFieldConfigManifest";
import {
  css,
  customElement,
  html,
  property,
  repeat,
  state,
} from "@umbraco-cms/backoffice/external/lit";
import { FormPropertyBackofficeModel } from "../../api";
import { UmbChangeEvent } from "@umbraco-cms/backoffice/event";
import { UmbFormControlMixin } from "@umbraco-cms/backoffice/validation";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";

interface KeyValueItem {
  key: string;
  value: string;
}

@customElement("sf-key-value-pair-property")
export default class KeyValuePairProperty
  extends UmbFormControlMixin<string, typeof UmbLitElement, undefined>(
    UmbLitElement,
    undefined
  )
  implements IFormFieldConfigElement
{
  @property({ type: Object })
  public set field(value: FormPropertyBackofficeModel) {
    this._field = value;
    this._items = JSON.parse(this._field.value || "[]");
  }
  public get field() {
    return this._field;
  }
  private _field!: FormPropertyBackofficeModel;

  @state()
  private _items: KeyValueItem[] = [];

  #addItem() {
    this._items = [...this._items, { key: "", value: "" }];
    //this.value.value = JSON.stringify(this._items);

    this.dispatchChangeEvent();
  }

  dispatchChangeEvent() {
    this.value = JSON.stringify(this._items);
    this.dispatchEvent(new UmbChangeEvent());
  }

  render() {
    return html`
      <umb-property-layout .label=${this._field?.displayName}>
        <div slot="editor">
          ${repeat(
            this._items,
            (item) =>
              html`<div class="item">
                <uui-input
                  placeholder="Key"
                  .value=${item.key}
                  @change=${(e: any) => {
                    item.key = e.target.value;
                    this.dispatchChangeEvent();
                  }}
                ></uui-input
                ><uui-input
                  placeholder="Value"
                  .value=${item.value}
                  @change=${(e: any) => {
                    item.value = e.target.value;
                    this.dispatchChangeEvent();
                  }}
                ></uui-input>
              </div>`
          )}
          <uui-button @click=${this.#addItem} look="outline" class="add-item"
            >Add Item</uui-button
          >
        </div>
      </umb-property-layout>
    `;
  }

  static styles = css`
    .item {
      display: flex;
      margin-bottom: 8px;
      gap: 8px;
    }

    .add-item {
      width: 100%;
    }
  `;
}
