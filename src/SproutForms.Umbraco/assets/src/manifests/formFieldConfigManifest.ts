import { ManifestElement } from "@umbraco-cms/backoffice/extension-api";
import { FormPropertyBackofficeModel } from "../api";
import { UmbPropertyEditorUiElement } from "@umbraco-cms/backoffice/property-editor";

export interface IFormFieldConfigElement extends UmbPropertyEditorUiElement {
    field: FormPropertyBackofficeModel;
}

export interface FormFieldConfigManifest extends ManifestElement<IFormFieldConfigElement> {
    type: 'formFieldConfig';
    propertyTypeAlias: string;
}