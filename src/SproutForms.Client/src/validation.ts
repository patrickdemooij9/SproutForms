import type { FormClientField, FormClientModel, FormClientTexts, ValidationRule } from './api/types.gen';
import { calculateVariables } from './calculations';
import { getEntryScope, isFieldRequired, isFieldVisible, isPageVisible, toConditionText } from './conditions';
import { getEntries, getEntryPrefix, getGroupFields, getPageFields, isFieldGroup } from './pages';
import { Registry, type Lookup } from './registry';
import type { FormErrors, FormValues, FormVariables } from './types';

export interface ValidatorContext {
    field: FormClientField;
    // For a field in a field group's entry, the entry's values over the form's (see getEntryScope)
    values: FormValues;
}

/**
 * Checks one validation rule. The value is never empty: an empty field only fails "required". A field group, such as a
 * repeater, is the exception: its value is the number of entries the visitor filled in (see toFieldText), empty for none, and
 * its rules run for none too.
 */
export type Validator = (value: string, rule: ValidationRule, context: ValidatorContext) => boolean | Promise<boolean>;

function checkDate(value: string, limit: unknown, check: (input: Date, limit: Date) => boolean): boolean {
    const input = new Date(value);
    const limitDate = new Date(String(limit ?? ''));
    // A date that can't be read is left to the server
    if (isNaN(input.getTime()) || isNaN(limitDate.getTime())) return true;
    return check(input, limitDate);
}

// A field group's value: the number of entries filled in, where an empty value is none
function toItemCount(value: string | null | undefined): number {
    return Number(value || 0);
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
    maxDate: (value, rule) => checkDate(value, rule.value, (input, max) => input <= max),
    minItems: (value, rule) => toItemCount(value) >= Number(rule.value ?? 0),
    maxItems: (value, rule) => rule.value === null || rule.value === undefined || toItemCount(value) <= Number(rule.value)
};

/**
 * The validators every form uses unless its own registry replaces them: the built-in ones and those added with registerValidator.
 */
export const globalValidators = new Registry<Validator>();
for (const [type, validator] of Object.entries(builtInValidators)) {
    globalValidators.register(type, validator);
}

/**
 * Adds or replaces the validator for a rule type, such as one a custom field type returns from GetValidationRules.
 */
export function registerValidator(type: string, validator: Validator): void {
    globalValidators.register(type, validator);
}

/**
 * The text a field submits: a checkbox counts as filled in only when it is checked, and an upload by its file name. A field
 * group counts its filled-in entries, and is empty without any.
 */
export function toFieldText(field: FormClientField, value: unknown): string {
    if (field.type === 'checkbox') return value === true || value === 'true' ? 'true' : '';
    if (isFieldGroup(field)) {
        const count = countFilledEntries(field, value);
        return count === 0 ? '' : String(count);
    }
    return toConditionText(value);
}

/**
 * An entry the visitor left empty: none of its fields is filled in, with an unticked checkbox counting as empty. The server drops
 * these entries before validating, so they aren't validated and don't count towards minItems and maxItems.
 */
export function isBlankEntry(field: FormClientField, entry: FormValues): boolean {
    return getGroupFields(field).every(child => toFieldText(child, entry[child.alias]).trim() === '');
}

/**
 * The entries of a field group's value that aren't blank.
 */
export function countFilledEntries(field: FormClientField, value: unknown): number {
    return getEntries(value).filter(entry => !isBlankEntry(field, entry)).length;
}

/**
 * The first error of a field, or undefined when it is valid. Rule types without a registered validator are skipped: the server
 * checks every rule again. For a field in a field group's entry, pass the entry's scope (see getEntryScope) as values. A field
 * group's own rules are checked, not its entries: validateForm does those. Pass the form's variables (see calculateVariables)
 * when its conditions use them.
 */
export async function validateField(field: FormClientField, values: FormValues, texts: FormClientTexts, variables?: FormVariables, validators: Lookup<Validator> = globalValidators): Promise<string | undefined> {
    const value = toFieldText(field, values[field.alias]);
    const requiredRule = field.validationRules.find(rule => rule.type === 'required');

    if (value.trim() === '') {
        if (isFieldRequired(field, values, variables)) return requiredRule?.message ?? texts.required;
        // A field group without entries still has too few of them, as on the server
        if (!isFieldGroup(field)) return undefined;
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
    // Where the validators come from; the global ones by default
    validators?: Lookup<Validator>;
}

/**
 * Validates the fields the visitor can see: fields hidden by their conditions and fields on skipped pages aren't validated,
 * on the server either. The fields in a field group's entries are validated too, except in blank entries, and their errors are
 * keyed by path, such as "people[2].email", with the index as submitted.
 */
export async function validateForm(definition: FormClientModel, values: FormValues, options: ValidateFormOptions = {}): Promise<FormErrors> {
    const errors: FormErrors = {};
    const variables = calculateVariables(definition, values);
    const pages = definition.pages.filter(page => options.pageIndex === undefined ? isPageVisible(page, values, variables) : page.index === options.pageIndex);

    for (const page of pages) {
        await validateFields(getPageFields(page), values, values, variables, '', definition.texts, options.validators ?? globalValidators, errors);
    }
    return errors;
}

/**
 * Validates the fields of the form, or of one entry of a field group: its own values, and the scope its conditions see.
 */
async function validateFields(fields: FormClientField[], values: FormValues, scope: FormValues, variables: FormVariables, pathPrefix: string, texts: FormClientTexts, validators: Lookup<Validator>, errors: FormErrors): Promise<void> {
    for (const field of fields) {
        if (!isFieldVisible(field, scope, variables)) continue;

        const path = pathPrefix + field.alias;
        if (isFieldGroup(field)) {
            const entries = getEntries(values[field.alias]);
            for (let index = 0; index < entries.length; index++) {
                const entry = entries[index];
                if (isBlankEntry(field, entry)) continue;
                await validateFields(getGroupFields(field), entry, getEntryScope(entry, scope), variables, getEntryPrefix(path, index), texts, validators, errors);
            }
        }

        const error = await validateField(field, scope, texts, variables, validators);
        if (error) errors[path] = [error];
    }
}
