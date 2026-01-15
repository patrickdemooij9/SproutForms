import { customElement, html } from "@umbraco-cms/backoffice/external/lit";
import { UmbWorkspaceElement } from "@umbraco-cms/backoffice/workspace";

@customElement("sprout-forms-list")
export class SproutFormsListElement extends UmbWorkspaceElement {
    render() {
        return html`
            <umb-body-layout main-no-padding headline='Forms'>
                <umb-collection alias='sproutForms.collections.forms'></umb-collection>
            </umb-body-layout>
        `
    }
}

export default SproutFormsListElement;