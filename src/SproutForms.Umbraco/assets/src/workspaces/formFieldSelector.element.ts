import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";
import { css, html, LitElement } from "lit";
import { customElement, repeat, state } from "@umbraco-cms/backoffice/external/lit";
import { SproutFormsSource } from "../repositories/sproutFormsSource";
import { FormFieldTypeDto } from "../models";

@customElement("form-field-selector")
export class FormFieldSelector extends UmbElementMixin(LitElement) {

    @state()
    private fieldTypes: FormFieldTypeDto[] = [];

    constructor() {
        super();
        
        new SproutFormsSource(this).getFieldTypes().then(resp => {
            this.fieldTypes = resp.data;
        });
    }

    render() {
        return html`
        ${repeat(this.fieldTypes, fieldType => fieldType.alias, fieldType => html`
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