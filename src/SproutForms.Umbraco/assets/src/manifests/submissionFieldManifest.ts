import { ManifestElement } from "@umbraco-cms/backoffice/extension-api";
import { FormSubmissionValueBackofficeModel } from "../api";

export interface ISubmissionFieldElement extends HTMLElement {
    value: FormSubmissionValueBackofficeModel;
}

export interface SubmissionFieldManifest extends ManifestElement<ISubmissionFieldElement> {
    type: 'submissionField';
    fieldTypeAlias: string;
}