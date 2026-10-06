import type { ConditionDefinition, ConditionRule, FieldRule, FormClientField, FormClientPage } from './api/types.gen';
import type { FormValues, FormVariables } from './types';

/**
 * A value as the server compares it: text, with booleans as "true"/"false" and a missing value as empty.
 */
export function toConditionText(value: unknown): string {
    if (value === null || value === undefined) return '';
    if (typeof value === 'string') return value;
    if (typeof value === 'number' || typeof value === 'boolean') return String(value);
    if (typeof File !== 'undefined' && value instanceof File) return value.name;
    return JSON.stringify(value);
}

// The number at the start of a value, as parseFloat reads it; the server reads it the same way
export function parseNumber(value: string): number | undefined {
    const number = parseFloat(value);
    return isNaN(number) ? undefined : number;
}

// Undefined when the pattern is invalid, which fails both regex comparisons, as on the server
function matchesRegex(value: string, pattern: string): boolean | undefined {
    try {
        return new RegExp(pattern).test(value);
    } catch {
        return undefined;
    }
}

// What a rule compares with: its value, or the value of the field or variable it names
function getTarget(rule: ConditionRule, values: FormValues, variables: FormVariables): unknown {
    switch (rule.valueSource) {
        case 'Field':
            return values[toConditionText(rule.value)];
        case 'Variable':
            return variables[toConditionText(rule.value)];
        default:
            return rule.value;
    }
}

/**
 * Evaluates a condition against the values entered so far and the form's variables (see calculateVariables), the same way the
 * server does. No condition, or one without rules, holds.
 */
export function evaluateCondition(condition: ConditionDefinition | null | undefined, values: FormValues, variables: FormVariables = {}): boolean {
    if (!condition || !condition.rules || condition.rules.length === 0) return true;

    const results = condition.rules.map(rule => {
        const value = toConditionText(rule.variableAlias ? variables[rule.variableAlias] : values[rule.fieldAlias]);
        const target = toConditionText(getTarget(rule, values, variables));

        switch (rule.comparison) {
            case 'Equals':
                return value.toLowerCase() === target.toLowerCase();
            case 'NotEquals':
                return value.toLowerCase() !== target.toLowerCase();
            case 'Contains':
                return value.toLowerCase().includes(target.toLowerCase());
            case 'GreaterThan': {
                const greater = parseNumber(value), than = parseNumber(target);
                return greater !== undefined && than !== undefined && greater > than;
            }
            case 'LessThan': {
                const less = parseNumber(value), than = parseNumber(target);
                return less !== undefined && than !== undefined && less < than;
            }
            case 'IsEmpty':
                return value.trim() === '';
            case 'IsNotEmpty':
                return value.trim() !== '';
            case 'MatchesRegex':
                return matchesRegex(value, target) === true;
            case 'DoesNotMatchRegex':
                return matchesRegex(value, target) === false;
            default:
                return true;
        }
    });

    return (condition.operator || 'All') === 'All'
        ? results.every(it => it)
        : results.some(it => it);
}

/**
 * The values the conditions of a field in a field group's entry see: the entry's own values over the form's, so a rule on an alias
 * uses the field of the same entry, or else the form's field. For an entry of a nested group, pass the outer entry's scope as values.
 */
export function getEntryScope(entry: FormValues, values: FormValues): FormValues {
    return { ...values, ...entry };
}

/**
 * Whether a field's rules show it: hidden while any Hide rule holds and, when it has Show rules, only shown while one of them holds.
 */
export function isShownByRules(rules: FieldRule[] | null | undefined, values: FormValues, variables?: FormVariables): boolean {
    if (rules?.some(rule => rule.action === 'Hide' && evaluateCondition(rule.condition, values, variables))) return false;
    const showRules = rules?.filter(rule => rule.action === 'Show') ?? [];
    return showRules.length === 0 || showRules.some(rule => evaluateCondition(rule.condition, values, variables));
}

/**
 * Whether one of a field's Require rules holds.
 */
export function isRequiredByRules(rules: FieldRule[] | null | undefined, values: FormValues, variables?: FormVariables): boolean {
    return !!rules?.some(rule => rule.action === 'Require' && evaluateCondition(rule.condition, values, variables));
}

export function isFieldVisible(field: FormClientField, values: FormValues, variables?: FormVariables): boolean {
    return isShownByRules(field.rules, values, variables);
}

/**
 * Required on its own, or because one of its Require rules holds.
 */
export function isFieldRequired(field: FormClientField, values: FormValues, variables?: FormVariables): boolean {
    return field.required || isRequiredByRules(field.rules, values, variables);
}

export function isPageVisible(page: FormClientPage, values: FormValues, variables?: FormVariables): boolean {
    return evaluateCondition(page.visibility, values, variables);
}
