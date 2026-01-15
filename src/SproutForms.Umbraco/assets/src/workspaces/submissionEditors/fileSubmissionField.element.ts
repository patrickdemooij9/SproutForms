import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";
import { html, LitElement } from "lit";
import { ISubmissionFieldElement } from "../../manifests/submissionFieldManifest";
import { FormSubmissionValueBackofficeModel } from "../../api";
import { customElement, property } from "@umbraco-cms/backoffice/external/lit";

@customElement("sf-file-submission-field")
export default class FileSubmissionFieldElement extends UmbElementMixin(LitElement) implements ISubmissionFieldElement {
    
    @property({ type: Object })
    public value!: FormSubmissionValueBackofficeModel;

    protected render() {
        return html`<p><strong>${this.value?.name}:</strong> ${JSON.parse(this.value?.value).FileName}</p>`
    }
}