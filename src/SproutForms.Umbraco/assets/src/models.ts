import { FieldConditions, FormColumnBackofficeModel, FormRowBackofficeModel } from "./api";

export interface FormDefinitionDto {
  id?: string;
  alias: string;
  name: string;
  version?: number;

  layout: FormRowDto[];
  //submitOutcome: FormOutcomeDto;
}

export interface FormRowDto {
  columns: FormColumnDto[];
}

export interface FormColumnDto {
  width: number; // 1–12
  fieldAlias: string | null;
}

export interface FormFieldDto {
  alias: string;
  label: string;
  fieldTypeAlias: string;
  required: boolean;
  configuration: Record<string, any>;
  conditions?: any;
}


export type EditableFormField = {
    alias: string;
    label: string;
    fieldTypeId: string;
    required: boolean;
    configuration: any;
    conditions?: FieldConditions | null;
};

export type SelectedState = {
    field: string | null;
    column: FormColumnBackofficeModel | null;
    row: FormRowBackofficeModel | null;
}

export type SelectedResizeState = {
    column: FormColumnBackofficeModel | null;
    size: number
}

export type FormOverviewItem = {
    unique: string;
    entityType: string;

    id: string;
    name: string;
    source: number;
    totalSubmissions: number;
}

export type FormSubmissionOverviewItem = {
    unique: string;
    entityType: string;

    id: string;
    name: string;
}

export type FormSubmissionOverviewFilter = {
    formId: string;
}

export type FormSubmissionInfoModalItem = {
    submissionId: string;
}

export const SOURCE_UI = 0;
export const SOURCE_CODE = 1;