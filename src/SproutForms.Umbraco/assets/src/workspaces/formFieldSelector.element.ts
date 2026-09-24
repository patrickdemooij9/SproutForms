import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";
import { css, html, LitElement } from "lit";
import { customElement, repeat, state } from "@umbraco-cms/backoffice/external/lit";
import { SproutFormsSource } from "../repositories/sproutFormsSource";
import { FormDefinitionTypeDto, FormFieldTypeDto } from "../models";
import { SF_FORM_DETAIL_TOKEN_CONTEXT } from "./sproutFormsWorkspaceContext";

@customElement("form-field-selector")
export class FormFieldSelector extends UmbElementMixin(LitElement) {

    @state()
    private fieldTypes: FormFieldTypeDto[] = [];

    @state()
    private formType?: FormDefinitionTypeDto;

    constructor() {
        super();
        
        new SproutFormsSource(this).getFieldTypes().then(resp => {
            this.fieldTypes = resp.data;
        });

        this.consumeContext(SF_FORM_DETAIL_TOKEN_CONTEXT, (context) => {
            this.observe(context?.formType, (formType) => {
                this.formType = formType;
            });
        });
    }

    // Only the field types the form's type allows; none until the form type is known
    #getAllowedFieldTypes() {
        const formType = this.formType;
        if (!formType) return [];

        return this.fieldTypes.filter(fieldType => formType.allowedFieldTypeAliases.includes(fieldType.alias));
    }

    render() {
        return html`
        <div class="grid">
            ${repeat(this.#getAllowedFieldTypes(), fieldType => fieldType.alias, fieldType => html`
                <button
                    class="tile"
                    @click=${() => this.onAddField(fieldType)}>
                    <umb-icon name=${fieldType.icon}></umb-icon>
                    <span>${fieldType.displayName}</span>
                </button>`
            )}
        </div>`
    }

    onAddField(fieldType: FormFieldTypeDto) {
        this.dispatchEvent(new CustomEvent("add-field", {
            detail: fieldType,
            bubbles: true,
            composed: true
        }))
    };

    static styles = 
        css`
        .grid {
            display: grid;
            grid-template-columns: repeat(auto-fill, minmax(140px, 1fr));
            gap: var(--uui-size-space-3);
        }

        .tile {
            display: flex;
            flex-direction: column;
            align-items: center;
            justify-content: center;
            gap: var(--uui-size-space-2);
            min-height: 84px;
            padding: var(--uui-size-space-3);
            font: inherit;
            color: var(--uui-color-text);
            background-color: var(--uui-color-surface);
            border: 1px solid var(--uui-color-border);
            border-radius: calc(var(--uui-border-radius) * 2);
            cursor: pointer;
            transition: border-color 120ms, box-shadow 120ms, color 120ms;

            umb-icon {
                font-size: 1.5em;
                color: var(--uui-color-interactive);
            }

            &:hover,
            &:focus-visible {
                border-color: var(--uui-color-interactive-emphasis);
                box-shadow: var(--uui-shadow-depth-1);
                color: var(--uui-color-interactive-emphasis);
                outline: none;
            }
        }`
    
}
