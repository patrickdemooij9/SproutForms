import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";
import {
  LitElement,
  html,
  customElement,
  property,
  state,
  css,
} from "@umbraco-cms/backoffice/external/lit";
import type { UmbPropertyEditorUiElement } from "@umbraco-cms/backoffice/property-editor";
import { UmbChangeEvent } from "@umbraco-cms/backoffice/event";
import { SF_FORM_DETAIL_TOKEN_CONTEXT } from "../workspaces/sproutFormsWorkspaceContext";
import { FormDto } from "../models";

const SPECIAL_TOKENS = [
  { label: "{All Values}", alias: "AllValues" },
];

@customElement("sf-token-textarea")
export default class TokenAutocompleteTextareaElement
  extends UmbElementMixin(LitElement)
  implements UmbPropertyEditorUiElement
{
  @property({ type: String })
  public set value(value: string) {
    this._value = value || "";
    this._displayValue = this.#resolveDisplayValue(this._value);
  }
  public get value() {
    return this._value;
  }
  private _value: string = "";

  @state()
  private _displayValue: string = "";

  @state()
  private _showSuggestions = false;

  @state()
  private _suggestions: { label: string; alias: string; isValid: boolean }[] = [];

  @state()
  private _selectedIndex = 0;

  @state()
  private _dropdownPosition = { top: 0, left: 0 };

  @state()
  private _fieldTokens: { label: string; alias: string }[] = [];

  @state()
  private _unknownTokens: string[] = [];

  constructor() {
    super();
    this.#initContext();
  }

  async #initContext() {
    this.consumeContext(SF_FORM_DETAIL_TOKEN_CONTEXT, (context) => {
      context?.form.subscribe((form: FormDto) => {
        this.#updateFieldTokens(form);
        this._displayValue = this.#resolveDisplayValue(this._value);
      });
    });
  }

  #updateFieldTokens(form: FormDto) {
    if (!form?.definition?.fields) {
      this._fieldTokens = [];
      return;
    }

    this._fieldTokens = form.definition.fields.map(field => ({
      label: `{${field.label}}`,
      alias: field.alias
    }));
  }

  #getAllTokens(): { label: string; alias: string }[] {
    return [...SPECIAL_TOKENS, ...this._fieldTokens];
  }

  #validateTokens() {
    const allTokens = this.#getAllTokens();
    const validAliases = new Set(allTokens.map(t => t.alias));
    
    const tokenPattern = /\{([^}]+)\}/g;
    const foundTokens: string[] = [];
    let match;
    
    while ((match = tokenPattern.exec(this._value)) !== null) {
      foundTokens.push(match[1]);
    }

    this._unknownTokens = foundTokens.filter(alias => !validAliases.has(alias));
  }

  #resolveDisplayValue(actualValue: string): string {
    if (!actualValue) return "";
    
    const allTokens = this.#getAllTokens();
    let display = actualValue;
    
    for (const token of allTokens) {
      if (actualValue.includes(`{${token.alias}}`)) {
        display = display.replace(`{${token.alias}}`, token.label);
      }
    }
    return display;
  }

  #resolveActualValue(displayValue: string): string {
    const allTokens = this.#getAllTokens();
    let actual = displayValue;
    
    for (const token of allTokens) {
      if (displayValue.includes(token.label)) {
        actual = actual.replace(token.label, `{${token.alias}}`);
      }
    }
    return actual;
  }

  #updateDropdownPosition() {
    const textarea = this.shadowRoot?.querySelector("textarea") as HTMLTextAreaElement;
    if (!textarea) return;

    const cursorPos = textarea.selectionStart;
    const textBeforeCursor = this._displayValue.substring(0, cursorPos);
    const lastTokenMatch = textBeforeCursor.match(/\{([^}]*)$/);

    if (!lastTokenMatch) {
      this._dropdownPosition = { top: 0, left: 0 };
      return;
    }

    const tokenStart = cursorPos - lastTokenMatch[0].length;
    
    const lines = this._displayValue.substring(0, tokenStart).split('\n');
    const lineHeight = 20;
    const charWidth = 8;
    
    let top = (lines.length - 1) * lineHeight + 8;
    let left = (lines[lines.length - 1].length) * charWidth + 8;

    const dropdownWidth = 220;
    const dropdownHeight = 200;
    const padding = 10;

    const textareaRect = textarea.getBoundingClientRect();
    const container = this.shadowRoot?.querySelector('.token-textarea-container') as HTMLElement;
    const containerRect = container?.getBoundingClientRect() || textareaRect;

    if (left + dropdownWidth > containerRect.width - padding) {
      left = Math.max(padding, containerRect.width - dropdownWidth - padding);
    }

    if (top + dropdownHeight > textareaRect.height - padding) {
      top = -dropdownHeight + 24;
    }

    this._dropdownPosition = { top, left };
  }

  #scrollDropdownToSelection() {
    const dropdown = this.shadowRoot?.querySelector('.suggestions-list') as HTMLElement;
    const selectedItem = this.shadowRoot?.querySelector('.suggestion-item.selected') as HTMLElement;
    
    if (!dropdown || !selectedItem) return;

    const dropdownRect = dropdown.getBoundingClientRect();
    const selectedRect = selectedItem.getBoundingClientRect();
    const itemHeight = selectedRect.height || 40;
    const offset = itemHeight;

    if (selectedRect.bottom > dropdownRect.bottom - offset) {
      dropdown.scrollTop += selectedRect.bottom - dropdownRect.bottom + offset;
    } else if (selectedRect.top < dropdownRect.top + offset) {
      dropdown.scrollTop += selectedRect.top - dropdownRect.top - offset;
    }
  }

  #handleInput(e: Event) {
    const textarea = e.target as HTMLTextAreaElement;
    const value = textarea.value;
    const cursorPos = textarea.selectionStart;

    this._displayValue = value;
    this._selectedIndex = 0;
    this._unknownTokens = [];
    
    const textBeforeCursor = value.substring(0, cursorPos);
    const lastTokenMatch = textBeforeCursor.match(/\{([^}]*)$/);

    if (lastTokenMatch) {
      const searchTerm = lastTokenMatch[1].toLowerCase();
      const allTokens = this.#getAllTokens();
      
      this._suggestions = allTokens
        .filter(m =>
          m.label.toLowerCase().includes(searchTerm) ||
          m.alias.toLowerCase().includes(searchTerm)
        )
        .map(token => ({
          ...token,
          isValid: true
        }));
      
      this._showSuggestions = this._suggestions.length > 0;
      this.#updateDropdownPosition();
    } else {
      this._showSuggestions = false;
    }

    this._value = this.#resolveActualValue(value);
    this.dispatchEvent(new UmbChangeEvent());
  }

  #handleKeyDown(e: KeyboardEvent) {
    if (e.key === "ArrowDown") {
      if (this._showSuggestions && this._suggestions.length > 0) {
        e.preventDefault();
        this._selectedIndex = (this._selectedIndex + 1) % this._suggestions.length;
        this.#scrollDropdownToSelection();
      }
    } else if (e.key === "ArrowUp") {
      if (this._showSuggestions && this._suggestions.length > 0) {
        e.preventDefault();
        this._selectedIndex = (this._selectedIndex - 1 + this._suggestions.length) % this._suggestions.length;
        this.#scrollDropdownToSelection();
      }
    } else if (e.key === "Enter" || e.key === "Tab") {
      if (this._showSuggestions && this._suggestions.length > 0) {
        e.preventDefault();
        this.#insertToken(this._suggestions[this._selectedIndex]);
      }
    } else if (e.key === "Escape") {
      this._showSuggestions = false;
    } else if (e.key === "}") {
      this._showSuggestions = false;
    }
  }

  #insertToken(token: { label: string; alias: string }) {
    const textarea = this.shadowRoot?.querySelector("textarea") as HTMLTextAreaElement;
    if (!textarea) return;

    const value = textarea.value;
    const cursorPos = textarea.selectionStart;
    
    const textBeforeCursor = value.substring(0, cursorPos);
    const textAfterCursor = value.substring(cursorPos);
    
    const lastTokenMatch = textBeforeCursor.match(/\{([^}]*)$/);
    
    if (lastTokenMatch) {
      const tokenStart = cursorPos - lastTokenMatch[0].length;
      const newTextBefore = textBeforeCursor.substring(0, tokenStart) + token.label;
      const newValue = newTextBefore + textAfterCursor;
      
      this._displayValue = newValue;
      this._value = this.#resolveActualValue(newValue);
      
      requestAnimationFrame(() => {
        const newCursorPos = tokenStart + token.label.length;
        textarea.setSelectionRange(newCursorPos, newCursorPos);
      });
      
      this._showSuggestions = false;
      this._selectedIndex = 0;
      this._unknownTokens = [];
      this.dispatchEvent(new UmbChangeEvent());
    }
  }

  #handleBlur() {
    this.#validateTokens();
    setTimeout(() => {
      this._showSuggestions = false;
    }, 200);
  }

  #handleFocus() {
    const textarea = this.shadowRoot?.querySelector("textarea") as HTMLTextAreaElement;
    if (!textarea) return;

    const cursorPos = textarea.selectionStart;
    const textBeforeCursor = this._displayValue.substring(0, cursorPos);
    const lastTokenMatch = textBeforeCursor.match(/\{([^}]*)$/);

    if (lastTokenMatch) {
      const searchTerm = lastTokenMatch[1].toLowerCase();
      const allTokens = this.#getAllTokens();
      
      this._suggestions = allTokens
        .filter(m =>
          m.label.toLowerCase().includes(searchTerm) ||
          m.alias.toLowerCase().includes(searchTerm)
        )
        .map(token => ({
          ...token,
          isValid: true
        }));
      
      this._showSuggestions = this._suggestions.length > 0;
      this.#updateDropdownPosition();
    }
  }

  override render() {
    return html`
      <div class="token-textarea-container">
        <textarea
          class="${this._unknownTokens.length > 0 ? 'has-warning' : ''}"
          .value=${this._displayValue}
          @input=${this.#handleInput}
          @keydown=${this.#handleKeyDown}
          @blur=${this.#handleBlur}
          @focus=${this.#handleFocus}
        ></textarea>
        
        ${this._showSuggestions ? html`
          <ul 
            class="suggestions-list"
            style="top: ${this._dropdownPosition.top}px; left: ${this._dropdownPosition.left}px;"
          >
            ${this._suggestions.map(
              (token, index) => html`
                <li 
                  class="suggestion-item ${index === this._selectedIndex ? 'selected' : ''}"
                  @click=${() => this.#insertToken(token)}
                  @mouseenter=${() => this._selectedIndex = index}
                >
                  <span class="token-label">${token.label}</span>
                  <span class="token-alias">(${token.alias})</span>
                </li>
              `
            )}
          </ul>
        ` : ''}
      </div>

      ${this._unknownTokens.length > 0 ? html`
        <div class="warning-message">
          <uui-icon name="icon-warning"></uui-icon>
          <span>Warning: The following tokens refer to fields that no longer exist: ${this._unknownTokens.map(t => `{${t}}`).join(', ')}</span>
        </div>
      ` : ''}
    `;
  }

  static styles = css`
    .token-textarea-container {
      position: relative;
      width: 100%;
    }

    textarea {
      width: 100%;
      min-height: 100px;
      padding: 8px;
      border: 1px solid var(--uui-color-border);
      border-radius: 4px;
      font-family: inherit;
      font-size: inherit;
      resize: vertical;
      box-sizing: border-box;
    }

    textarea:focus {
      outline: none;
      border-color: var(--uui-color-focus);
    }

    textarea.has-warning {
      border-color: #f59e0b;
      background-color: rgba(245, 158, 11, 0.05);
    }

    .suggestions-list {
      position: absolute;
      background: var(--uui-color-surface);
      border: 1px solid var(--uui-color-border);
      border-radius: 8px;
      list-style: none;
      margin: 0;
      padding: 4px 0;
      max-height: 200px;
      overflow-y: auto;
      z-index: 100;
      box-shadow: 0 8px 16px rgba(0, 0, 0, 0.15), 0 2px 4px rgba(0, 0, 0, 0.1);
      min-width: 200px;
    }

    .suggestion-item {
      padding: 10px 14px;
      cursor: pointer;
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin: 0 4px;
      border-radius: 6px;
      transition: background-color 0.15s ease, transform 0.1s ease;
    }

    .suggestion-item.selected {
      background: linear-gradient(135deg, #0078D4 0%, #106EBE 100%);
      color: white;
    }

    .suggestion-item.selected .token-alias {
      color: rgba(255, 255, 255, 0.8);
    }

    .suggestion-item:hover {
      background: var(--uui-color-surface-hover);
      transform: translateX(2px);
    }

    .suggestion-item.selected:hover {
      background: linear-gradient(135deg, #106EBE 0%, #005A9E 100%);
    }

    .token-label {
      font-weight: 600;
      color: var(--uui-color-text);
    }

    .token-alias {
      color: var(--uui-color-text-alt);
      font-size: 0.85em;
      font-family: monospace;
    }

    .warning-message {
      display: flex;
      align-items: flex-start;
      gap: 8px;
      margin-top: 8px;
      padding: 10px 12px;
      background-color: rgba(245, 158, 11, 0.1);
      border: 1px solid rgba(245, 158, 11, 0.3);
      border-radius: 4px;
      color: #b45309;
      font-size: 0.85em;
      line-height: 1.4;
    }

    .warning-message uui-icon {
      flex-shrink: 0;
      margin-top: 2px;
    }
  `;
}

declare global {
  interface HTMLElementTagNameMap {
    "sf-token-textarea": TokenAutocompleteTextareaElement;
  }
}
