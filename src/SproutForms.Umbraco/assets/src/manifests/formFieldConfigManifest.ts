import { ManifestElement } from "@umbraco-cms/backoffice/extension-api";
import { UmbPropertyEditorUiElement } from "@umbraco-cms/backoffice/property-editor";
import { FormPropertyDto } from "../models";

export interface IFormFieldConfigElement extends UmbPropertyEditorUiElement {
    field: FormPropertyDto;
}

export interface FormFieldConfigManifest extends ManifestElement<IFormFieldConfigElement> {
    type: 'formFieldConfig';
    propertyTypeAlias: string;
}