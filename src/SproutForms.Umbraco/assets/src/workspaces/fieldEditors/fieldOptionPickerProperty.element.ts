import { IFormFieldConfigElement } from "../../manifests/formFieldConfigManifest";
import {
  css,
  customElement,
  html,
  property,
} from "@umbraco-cms/backoffice/external/lit";
import { UmbChangeEvent } from "@umbraco-cms/backoffice/event";
import { UmbFormControlMixin } from "@umbraco-cms/backoffice/validation";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { FormFieldDto, FormPropertyDto } from "../../models";

interface OptionItem {
  key: string;
  value: string;
}

/**
 * Picks one of the options of the field being edited, such as the correct answer of a quiz question.
 * Works for field types that keep their options under "options", like radio buttons and dropdowns.
 */
@customElement("sf-field-option-picker-property")
export default class FieldOptionPickerProperty
  extends UmbFormControlMixin<string, typeof UmbLitElement, undefined>(
    UmbLitElement,
    undefined
  )
  implements IFormFieldConfigElement
{
  @property({ type: Object })
  public set field(value: FormPropertyDto) {
    this._field = value;
    this.value = (value.value as string) ?? "";
  }
  public get field() {
    return this._field;
  }
  private _field!: FormPropertyDto;

  @property({ type: Object })
  public formField?: FormFieldDto;

  #getOptions(): OptionItem[] {
    const options = this.formField?.configuration["options"];
    if (Array.isArray(options)) return options;
    try {
      return JSON.parse((options as string) || "[]");
    } catch {
      return [];
    }
  }

  #onChange(e: Event) {
    this.value = (e.target as HTMLSelectElement).value;
    this.dispatchEvent(new UmbChangeEvent());
  }

  render() {
    const options = this.#getOptions().filter((option) => option.value);

    return html`
      <umb-property-layout .label=${this._field?.displayName}>
        <div slot="editor">
          ${options.length === 0
            ? html`<p class="hint">
                Add options to this field on the General tab first.
              </p>`
            : html`
                <select class="uui-select" @change=${this.#onChange}>
                  <option value="" ?selected=${!this.value}>Choose an option</option>
                  ${options.map(
                    (option) => html`<option
                      value=${option.value}
                      ?selected=${option.value === this.value}
                    >
                      ${option.key || option.value}
                    </option>`
                  )}
                </select>
              `}
        </div>
      </umb-property-layout>
    `;
  }

  static styles = css`
    select {
      width: 100%;
      padding: var(--uui-size-2) var(--uui-size-3);
      border: 1px solid var(--uui-color-border);
      border-radius: var(--uui-border-radius);
      background-color: var(--uui-color-surface);
      color: var(--uui-color-text);
      font: inherit;
    }

    .hint {
      margin: 0;
      color: var(--uui-color-text-alt);
    }
  `;
}
