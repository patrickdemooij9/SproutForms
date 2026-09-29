import type { FormClientField, FormClientModel, FormClientTexts, ValidationRule } from './api/types.gen';
import { isFieldRequired, isFieldVisible, isPageVisible, toConditionText } from './conditions';
import { getPageFields } from './pages';
import type { FormErrors, FormValues } from './types';

export interface ValidatorContext {
    field: FormClientField;
    values: FormValues;
}

/**
 * Checks one validation rule. The value is never empty: an empty field only fails "required".
 */
export type Validator = (value: string, rule: ValidationRule, context: ValidatorContext) => boolean | Promise<boolean>;

function checkDate(value: string, limit: unknown, check: (input: Date, limit: Date) => boolean): boolean {
    const input = new Date(value);
    const limitDate = new Date(String(limit ?? ''));
    // A date that can't be read is left to the server
    if (isNaN(input.getTime()) || isNaN(limitDate.getTime())) return true;
    return check(input, limitDate);
}

/**
 * The rules SproutForms' field types produce. forms.js uses them too, so Razor and headless forms validate the same way.
 */
export const builtInValidators: Readonly<Record<string, Validator>> = {
    required: value => value.trim().length > 0,
    minLength: (value, rule) => value.length >= Number(rule.value ?? 0),
    maxLength: (value, rule) => rule.value === null || rule.value === undefined || value.length <= Number(rule.value),
    regex: (value, rule) => {
        if (!rule.value) return true;
        try {
            return new RegExp(String(rule.value)).test(value);
        } catch {
            return false;
        }
    },
    minDate: (value, rule) => checkDate(value, rule.value, (input, min) => input >= min),
    maxDate: (value, rule) => checkDate(value, rule.value, (input, max) => input <= max)
};

const validators = new Map<string, Validator>(Object.entries(builtInValidators));

/**
 * Adds or replaces the validator for a rule type, such as one a custom field type returns from GetValidationRules.
 */
export function registerValidator(type: string, validator: Validator): void {
    validators.set(type, validator);
}

/**
 * The text a field submits: a checkbox counts as filled in only when it is checked, and an upload by its file name.
 */
export function toFieldText(field: FormClientField, value: unknown): string {
    if (field.type === 'checkbox') return value === true || value === 'true' ? 'true' : '';
    return toConditionText(value);
}

/**
 * The first error of a field, or undefined when it is valid. Rule types without a registered validator are skipped: the server
 * checks every rule again.
 */
export async function validateField(field: FormClientField, values: FormValues, texts: FormClientTexts): Promise<string | undefined> {
    const value = toFieldText(field, values[field.alias]);
    const requiredRule = field.validationRules.find(rule => rule.type === 'required');

    if (value.trim() === '') {
        return isFieldRequired(field, values) ? requiredRule?.message ?? texts.required : undefined;
    }

    for (const rule of field.validationRules) {
        if (rule.type === 'required') continue;

        const validator = validators.get(rule.type);
        if (!validator) continue;

        if (!await validator(value, rule, { field, values })) {
            return rule.message ?? texts.invalid;
        }
    }
    return undefined;
}

export interface ValidateFormOptions {
    // Only the fields of this page, as when the visitor goes to the next page
    pageIndex?: number;
}

/**
 * Validates the fields the visitor can see: fields hidden by their conditions and fields on skipped pages aren't validated,
 * on the server either.
 */
export async function validateForm(definition: FormClientModel, values: FormValues, options: ValidateFormOptions = {}): Promise<FormErrors> {
    const errors: FormErrors = {};
    const pages = definition.pages.filter(page => options.pageIndex === undefined ? isPageVisible(page, values) : page.index === options.pageIndex);

    for (const page of pages) {
        for (const field of getPageFields(page)) {
            if (!isFieldVisible(field, values)) continue;

            const error = await validateField(field, values, definition.texts);
            if (error) errors[field.alias] = [error];
        }
    }
    return errors;
}
