import { CalculationRule, ConditionDefinition, FieldRule, FormVariable } from "./api";

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
  type: FormTypeSelectionDto;
  pages: Array<FormPageDto>;
  fields: Array<FormFieldDto>;
  outcome: FormOutcomeDto;
  // Checked top to bottom after submitting; the first whose condition holds replaces the outcome
  conditionalOutcomes: Array<FormConditionalOutcomeDto>;
  variables: Array<FormVariableDto>;
  // Run top to bottom, so their order matters
  calculations: Array<CalculationRuleDto>;
  workflows: Array<FormWorkflowDto>;
  submitLabel?: string | null;
  showProgress: boolean;
}

export interface FormPageDto {
  id: string;
  title?: string | null;
  rows: Array<FormRowDto>;
  nextLabel?: string | null;
  previousLabel?: string | null;
  visibility?: ConditionDefinition | null;
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
  // When the field shows, hides or is required. Its rules that change a variable are calculations it owns
  rules: Array<FieldRule>;
  extension?: {
    [key: string]: unknown;
  } | null;
  // Set for a field group such as a repeater: its own fields, and how one entry lays them out
  fields?: Array<FormFieldDto> | null;
  rows?: Array<FormRowDto> | null;
}

// Where a field's rows live: a page of the form, or a field group such as a repeater
export type FieldContainer =
  | { kind: "page"; pageIndex: number }
  | { kind: "group"; groupId: string };

export interface FormWorkflowDto {
  id: string;
  alias: string;
  typeAlias: string;
  displayName: string;
  order: number;
  configuration: {
    [key: string]: unknown;
  };
  templateId?: string | null;
}

export type FormOutcomeDto = {
  typeAlias: string;
  displayName: string;
  configuration: {
    [key: string]: unknown;
  };
};

// The ids only exist in the editor, so the lists can be reordered and variables renamed while editing
export type FormConditionalOutcomeDto = {
  id: string;
  condition: ConditionDefinition;
  outcome: FormOutcomeDto;
};

export type FormVariableDto = FormVariable & { id: string };

export type CalculationRuleDto = CalculationRule & { id: string };

export type FormTypeSelectionDto = {
  typeAlias: string;
  displayName: string;
  settings: {
    [key: string]: unknown;
  };
};

export type FormDefinitionTypeDto = {
  alias: string;
  displayName: string;
  description: string;
  properties: Array<FormPropertyDto>;
  allowedFieldTypeAliases: Array<string>;
  allowedOutcomeTypeAliases: Array<string>;
  fieldExtensions: Array<FormFieldExtensionDto>;
};

export type FormFieldExtensionDto = {
  fieldTypeAlias: string;
  properties: Array<FormPropertyDto>;
};

export type FormTypePickerModalData = {
  formTypes: Array<FormDefinitionTypeDto>;
};

export type FormTypePickerModalValue = {
  formTypeAlias: string;
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
  // A field group, such as a repeater, holds fields of its own
  isFieldGroup: boolean;
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
  // The page or field group the row belongs to; the current page when not set
  container?: FieldContainer;
  // The inspector shows the current page's settings instead of the field list
  pageSettings?: boolean;
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

export const TRASHED_FORM_ENTITY_TYPE = "sprout-trashed-form";

export type TrashedFormItem = {
  unique: string;
  entityType: typeof TRASHED_FORM_ENTITY_TYPE;

  name: string;
  alias: string;
  trashedAt: string;
  trashedByName: string;
  folderName?: string | null;
  totalSubmissions: number;
};

export const SUBMISSION_ENTITY_TYPE = "sprout-submission";
export const TRASHED_SUBMISSION_ENTITY_TYPE = "sprout-trashed-submission";

export type FormSubmissionOverviewItem = {
  unique: string;
  entityType: typeof SUBMISSION_ENTITY_TYPE | typeof TRASHED_SUBMISSION_ENTITY_TYPE;

  id: string;
  name: string;
  // Set for a submission in the recycle bin
  trashedAt?: string | null;
  trashedByName?: string | null;
  // The form's variables as calculated for the submission, with their values formatted
  variables?: Array<{
    alias: string;
    name: string;
    value: string;
  }>;
  workflowStages?: Array<{
    workflowAlias: string;
    displayName: string;
    order: number;
    status: string;
  }>;
};

export type FormSubmissionOverviewFilter = {
  formId: string;
};

export type FormSubmissionInfoModalItem = {
  submissionId: string;
};

export const SOURCE_UI = 0;
export const SOURCE_CODE = 1;

export interface WorkflowTemplateDto {
  id: string;
  name: string;
  workflowTypeAlias: string;
  configurationJson: string;
  lockedFields: string[];
}
