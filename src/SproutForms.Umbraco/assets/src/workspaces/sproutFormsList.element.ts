import { customElement, html } from "@umbraco-cms/backoffice/external/lit";
import { UmbWorkspaceElement } from "@umbraco-cms/backoffice/workspace";

@customElement("sprout-forms-list")
export class SproutFormsListElement extends UmbWorkspaceElement {
    render() {
        return html`
            <umb-collection alias='sproutForms.collections.forms'></umb-collection>
        `
    }
}

export default SproutFormsListElement;