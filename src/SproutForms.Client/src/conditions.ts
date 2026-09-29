import type { ConditionDefinition, FormClientField, FormClientPage } from './api/types.gen';
import type { FormValues } from './types';

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
function parseNumber(value: string): number | undefined {
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

/**
 * Evaluates a condition against the values entered so far, the same way the server does. No condition, or one without rules, holds.
 */
export function evaluateCondition(condition: ConditionDefinition | null | undefined, values: FormValues): boolean {
    if (!condition || !condition.rules || condition.rules.length === 0) return true;

    const results = condition.rules.map(rule => {
        const value = toConditionText(values[rule.fieldAlias]);
        const target = toConditionText(rule.value);

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

export function isFieldVisible(field: FormClientField, values: FormValues): boolean {
    return evaluateCondition(field.conditions?.visibility, values);
}

/**
 * Required on its own, or because its required condition holds.
 */
export function isFieldRequired(field: FormClientField, values: FormValues): boolean {
    return field.required || (!!field.conditions?.required && evaluateCondition(field.conditions.required, values));
}

export function isPageVisible(page: FormClientPage, values: FormValues): boolean {
    return evaluateCondition(page.visibility, values);
}
