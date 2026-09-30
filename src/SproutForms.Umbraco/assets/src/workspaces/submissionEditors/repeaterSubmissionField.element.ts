import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";
import { css, html, LitElement } from "lit";
import { ISubmissionFieldElement } from "../../manifests/submissionFieldManifest";
import { FormSubmissionValueBackofficeModel } from "../../api";
import { customElement, property, repeat } from "@umbraco-cms/backoffice/external/lit";

import "./submissionField.element";

// Each entry's values go through the submission field extensions again, so a file inside an entry shows like any other file
@customElement("sf-repeater-submission-field")
export default class RepeaterSubmissionFieldElement extends UmbElementMixin(LitElement) implements ISubmissionFieldElement {

    @property({ type: Object })
    public value!: FormSubmissionValueBackofficeModel;

    protected render() {
        const entries = this.value?.entries ?? [];
        return html`
            <p><strong>${this.value?.name}:</strong> ${entries.length === 0 ? "No entries" : ""}</p>
            ${repeat(entries, (_entry, index) => index, (entry) => html`
                <div class="entry">
                    <h4>${entry.title}</h4>
                    ${repeat(entry.values, (_value, index) => index, (value) => html`
                        <sf-submission-field .fieldValue=${value}></sf-submission-field>
                    `)}
                </div>
            `)}
        `;
    }

    static styles = css`
        .entry {
            margin: 0 0 var(--uui-size-space-4) var(--uui-size-space-4);
            padding: var(--uui-size-space-2) var(--uui-size-space-4);
            border-left: 3px solid var(--uui-color-border);
        }

        h4 {
            margin: var(--uui-size-space-2) 0;
        }
    `;
}
