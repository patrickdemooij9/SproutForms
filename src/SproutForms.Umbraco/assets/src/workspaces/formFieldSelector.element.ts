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
        ${repeat(this.#getAllowedFieldTypes(), fieldType => fieldType.alias, fieldType => html`
            <button
                @click=${() => this.onAddField(fieldType)}>
                ${fieldType.displayName}
            </button>`
        )}`
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
        button {
            display: block;
            width: 100%;
            margin-bottom: 8px;
            border: 1px solid #ccc;
            border-radius: 4px;
            background-color: transparent;
            padding: 8px 12px;
            cursor: pointer;

            &:hover {
                background-color: #e5e7eb;
            }
        }`
    
}