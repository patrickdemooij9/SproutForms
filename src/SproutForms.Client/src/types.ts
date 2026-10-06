import type { FormClientField } from './api/types.gen';

export type {
    CalculationOperand,
    CalculationOperation,
    CalculationRule,
    ConditionComparison,
    ConditionDefinition,
    ConditionRule,
    ConditionValueSource,
    FieldRule,
    FieldRuleAction,
    FormClientColumn,
    FormClientField,
    FormClientModel,
    FormClientPage,
    FormClientRow,
    FormClientSubmissionGuard,
    FormClientTexts,
    FormClientVariable,
    FormVariableType,
    HeadlessOutcome,
    HeadlessSubmitResponse,
    ValidationProblemDetails,
    ValidationRule
} from './api/types.gen';

/**
 * What the visitor entered, keyed by field alias. Texts, numbers and booleans are all fine; a File is sent as an upload. A field
 * group, such as a repeater, holds a list of entries (see RepeaterValue).
 */
export type FormValues = Record<string, unknown>;

/**
 * The form's variables as the browser works them out (see calculateVariables), keyed by variable alias: a number or text.
 */
export type FormVariables = Record<string, number | string>;

/**
 * The value of a field group such as a repeater: one object per entry, keyed by the alias of the fields in it, such as
 * [{ firstName: "Ann" }, { firstName: "Bob" }]. An entry can hold a File and field groups of its own.
 */
export type RepeaterValue = FormValues[];

/**
 * Error messages keyed by field path: a field's alias, or for a field in a field group's entry, its path such as "people[0].email".
 * A key that isn't a field, such as "submissionGuard", is about the whole form.
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

export interface RepeaterFieldConfiguration {
    minItems?: number | null;
    maxItems?: number | null;
    // The entries shown before the visitor adds any, already between minItems and maxItems
    initialItems: number;
    addLabel: string;
    removeLabel: string;
    // The heading of each entry, where {n} is its number; see getEntryTitle
    itemTitle?: string | null;
}

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
    repeater: RepeaterFieldConfiguration;
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
