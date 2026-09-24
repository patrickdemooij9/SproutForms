import { ManifestElement } from "@umbraco-cms/backoffice/extension-api";
import { UmbPropertyEditorUiElement } from "@umbraco-cms/backoffice/property-editor";
import { FormFieldDto, FormPropertyDto } from "../models";

export interface IFormFieldConfigElement extends UmbPropertyEditorUiElement {
    field: FormPropertyDto;
    // The field being edited, for an editor that depends on its other settings
    formField?: FormFieldDto;
}

export interface FormFieldConfigManifest extends ManifestElement<IFormFieldConfigElement> {
    type: 'formFieldConfig';
    propertyTypeAlias: string;
}