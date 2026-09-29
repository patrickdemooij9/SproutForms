import type { FormClientField } from './api/types.gen';

export type {
    ConditionComparison,
    ConditionDefinition,
    ConditionRule,
    FieldConditions,
    FormClientColumn,
    FormClientField,
    FormClientModel,
    FormClientPage,
    FormClientRow,
    FormClientSubmissionGuard,
    FormClientTexts,
    HeadlessOutcome,
    HeadlessSubmitRequest,
    HeadlessSubmitResponse,
    HeadlessValidatePageRequest,
    ValidationProblemDetails,
    ValidationRule
} from './api/types.gen';

/**
 * What the visitor entered, keyed by field alias. Texts, numbers and booleans are all fine; a File is sent as an upload.
 */
export type FormValues = Record<string, unknown>;

/**
 * Error messages keyed by field alias. A key that isn't a field, such as "submissionGuard", is about the whole form.
 */
export type FormErrors = Record<string, string[]>;

export interface FieldOption {
    value: string;
    label: string;
}

export interface TextFieldConfiguration {
    placeholder?: string | null;
    minLength?: number | null;
    maxLength?: number | null;
    regex?: string | null;
}

export interface EmailFieldConfiguration {
    placeholder?: string | null;
}

export interface TextAreaFieldConfiguration {
    rows: number;
    maxLength?: number | null;
}

export interface OptionsFieldConfiguration {
    options: FieldOption[];
}

export interface DateFieldConfiguration {
    // ISO 8601
    min?: string | null;
    max?: string | null;
    includeTime: boolean;
}

export interface FileFieldConfiguration {
    maxFileSizeBytes: number;
    // Lower case, with the dot, such as ".pdf"; null allows every extension
    allowedExtensions?: string[] | null;
}

export interface HiddenFieldConfiguration {
    defaultValue?: string | null;
    allowOverrideFromClient: boolean;
}

export type CheckboxFieldConfiguration = Record<string, never>;

/**
 * The configuration of each field type SproutForms ships. A custom field type's configuration is whatever its
 * GetClientConfiguration returns; narrow it yourself.
 */
export interface BuiltInFieldConfigurations {
    text: TextFieldConfiguration;
    email: EmailFieldConfiguration;
    textarea: TextAreaFieldConfiguration;
    select: OptionsFieldConfiguration;
    radio: OptionsFieldConfiguration;
    date: DateFieldConfiguration;
    file: FileFieldConfiguration;
    hidden: HiddenFieldConfiguration;
    checkbox: CheckboxFieldConfiguration;
}

export type BuiltInFieldType = keyof BuiltInFieldConfigurations;

export type TypedField<T extends BuiltInFieldType> = Omit<FormClientField, 'type' | 'configuration'> & {
    type: T;
    configuration: BuiltInFieldConfigurations[T];
};

/**
 * Narrows a field to one of the built-in field types, typing its configuration.
 */
export function isFieldOfType<T extends BuiltInFieldType>(field: FormClientField, type: T): field is FormClientField & TypedField<T> {
    return field.type === type;
}
