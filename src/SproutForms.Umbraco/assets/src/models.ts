import { FieldConditions } from "./api";

export interface FormDto {
  id?: string | null;
  folderId?: string | null;
  name: string;
  alias: string;
  version: number;
  source: number;
  definition: FormDefinitionDto;
}

export interface FormDefinitionDto {
  rows: Array<FormRowDto>;
  fields: Array<FormFieldDto>;
  outcome: FormOutcomeDto;
  workflows: Array<FormWorkflowDto>;
}

export interface FormRowDto {
  id: string;
  columns: FormColumnDto[];
}

export interface FormColumnDto {
  id: string;
  width: number; // 1–12
  fieldId: string | null;
}

export interface FormFieldDto {
  id: string;
  alias: string;
  label: string;
  fieldTypeAlias: string;
  required: boolean;
  configuration: {
    [key: string]: unknown;
  };
  conditions?: FieldConditions | null;
}

export interface FormWorkflowDto {
  id: string;
  alias: string;
  typeAlias: string;
  displayName: string;
  order: number;
  configuration: {
    [key: string]: unknown;
  };
}

export type FormOutcomeDto = {
  typeAlias: string;
  displayName: string;
  configuration: {
    [key: string]: unknown;
  };
};

export type FormOutcomeTypeDto = {
  alias: string;
  displayName: string;
  properties: Array<FormPropertyDto>;
};

export interface FormFieldTypeDto {
  alias: string;
  displayName: string;
  icon: string;
  properties: Array<FormPropertyDto>;
}

export interface FormFlowTypeDto {
  alias: string;
  displayName: string;
  configuration: Array<FormPropertyDto>;
}

export interface FormPropertyDto {
  alias: string;
  displayName: string;
  propertyEditor: string;
  value?: unknown;
}

export type SelectedState = {
  field: string | null;
  column: FormColumnDto | null;
  row: FormRowDto | null;
};

export type SelectedResizeState = {
  column: FormColumnDto | null;
  size: number;
};

export type FormOverviewItem = {
  unique: string;
  entityType: string;

  id: string;
  name: string;
  source: number;
  totalSubmissions: number;
};

export type FormSubmissionOverviewItem = {
  unique: string;
  entityType: string;

  id: string;
  name: string;
};

export type FormSubmissionOverviewFilter = {
  formId: string;
};

export type FormSubmissionInfoModalItem = {
  submissionId: string;
};

export const SOURCE_UI = 0;
export const SOURCE_CODE = 1;
