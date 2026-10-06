import type { CalculationOperand, CalculationRule, ConditionDefinition, FormClientField, FormClientModel, FormClientRow, FormClientVariable } from './api/types.gen';
import { evaluateCondition, isFieldVisible, isPageVisible, parseNumber, toConditionText } from './conditions';
import type { FormValues, FormVariables } from './types';

// The most decimals a number keeps, as on the server
const maxDecimals = 10;

function getRowFields(rows: FormClientRow[]): FormClientField[] {
    return rows.flatMap(row => row.columns).map(column => column.field);
}

// A value that isn't a number counts as 0, as on the server
function toNumber(value: unknown): number {
    if (typeof value === 'number') return Number.isFinite(value) ? value : 0;
    return parseNumber(toConditionText(value)) ?? 0;
}

// Without the float noise of binary numbers, such as 0.1 + 0.2, so a number compares as the server's decimal does
function clean(number: number): number {
    return Number(number.toPrecision(15));
}

// Half away from zero, as the server rounds
function round(number: number, decimals: number): number {
    const places = Math.min(Math.max(decimals, 0), maxDecimals);
    const rounded = Number(`${Math.round(Number(`${Math.abs(number)}e${places}`))}e-${places}`);
    return clean(Math.sign(number) * rounded) || 0;
}

function getInitialValue(variable: FormClientVariable): number | string {
    return variable.type === 'Text' ? toConditionText(variable.initialValue) : clean(toNumber(variable.initialValue));
}

function getInitialVariables(variables: FormClientVariable[]): FormVariables {
    return Object.fromEntries(variables.map(variable => [variable.alias, getInitialValue(variable)]));
}

function resolve(operand: CalculationOperand, values: FormValues, variables: FormVariables): unknown {
    switch (operand.source) {
        case 'Field':
            return values[toConditionText(operand.value)];
        case 'Variable':
            return variables[toConditionText(operand.value)];
        default:
            return operand.value;
    }
}

// An operation that doesn't fit the variable's type, or a division by zero, leaves it as it was
function apply(variable: FormClientVariable, current: number | string, rule: CalculationRule, operand: unknown): number | string {
    if (variable.type === 'Text') {
        switch (rule.operation) {
            case 'Set': return toConditionText(operand);
            case 'Append': return toConditionText(current) + toConditionText(operand);
            default: return current;
        }
    }

    const number = toNumber(current), by = toNumber(operand);
    let result: number;
    switch (rule.operation) {
        case 'Set': result = by; break;
        case 'Add': result = number + by; break;
        case 'Subtract': result = number - by; break;
        case 'Multiply': result = number * by; break;
        case 'Divide': result = by === 0 ? number : number / by; break;
        default: return current;
    }
    return Number.isFinite(result) ? clean(result) : current;
}

/**
 * Runs the calculation rules top to bottom on the values once, and rounds the numbers to their variable's decimals.
 */
function runCalculations(definitions: FormClientVariable[], calculations: CalculationRule[], values: FormValues): FormVariables {
    const variables = getInitialVariables(definitions);
    for (const rule of calculations) {
        const variable = definitions.find(it => it.alias === rule.variableAlias);
        if (!variable || !evaluateCondition(rule.condition, values, variables)) continue;
        variables[variable.alias] = apply(variable, variables[variable.alias], rule, resolve(rule.operand, values, variables));
    }

    for (const variable of definitions.filter(it => it.type !== 'Text')) {
        variables[variable.alias] = round(toNumber(variables[variable.alias]), variable.decimals);
    }
    return variables;
}

/**
 * True when a condition reads a variable.
 */
export function usesVariables(condition: ConditionDefinition | null | undefined): boolean {
    return !!condition?.rules?.some(rule => !!rule.variableAlias || rule.valueSource === 'Variable');
}

/**
 * Works out variables from the values the way the server does, for a renderer that decides visibility itself, such as forms.js.
 * A field the visitor doesn't see counts as empty: getHiddenFields gives the aliases of the fields hidden by their own conditions
 * or a skipped page with the variables so far. When dependsOnVisibility is false, no condition reads a variable, and one pass does.
 */
export function computeVariables(
    definitions: FormClientVariable[],
    calculations: CalculationRule[],
    values: FormValues,
    getHiddenFields: (variables: FormVariables) => Iterable<string>,
    dependsOnVisibility: boolean
): FormVariables {
    if (definitions.length === 0) return {};

    const withoutHidden = (variables: FormVariables): FormValues => {
        const visible = { ...values };
        for (const alias of getHiddenFields(variables)) delete visible[alias];
        return visible;
    };

    // What's visible can depend on the variables, and the variables on what's visible. The server rules out cycles, so each
    // pass settles at least one more variable, and it's done once a pass changes nothing
    let variables = runCalculations(definitions, calculations, withoutHidden(getInitialVariables(definitions)));
    for (let pass = 0; dependsOnVisibility && pass < definitions.length; pass++) {
        const next = runCalculations(definitions, calculations, withoutHidden(variables));
        if (JSON.stringify(next) === JSON.stringify(variables)) break;
        variables = next;
    }
    return variables;
}

// True when a field's or page's condition reads a variable, so what's visible can change with the calculations
function conditionsUseVariables(definition: FormClientModel): boolean {
    const fieldsUse = (fields: FormClientField[]): boolean => fields.some(field =>
        field.rules.some(rule => usesVariables(rule.condition)) || fieldsUse(field.rows ? getRowFields(field.rows) : []));
    return definition.pages.some(page => usesVariables(page.visibility) || fieldsUse(getRowFields(page.rows)));
}

/**
 * Works out the form's variables from the values entered so far, the same way the server does. Only the variables the browser
 * needs are in the definition: those its field and page conditions use. Pass them to the condition functions,
 * such as isFieldVisible; the page functions and validateForm work them out themselves.
 */
export function calculateVariables(definition: FormClientModel, values: FormValues): FormVariables {
    if (!definition.variables?.length) return {};

    // A field hidden by its own conditions or on a skipped page
    const getHiddenFields = (variables: FormVariables): string[] => definition.pages.flatMap(page => {
        const pageVisible = isPageVisible(page, values, variables);
        return getRowFields(page.rows).filter(field => !pageVisible || !isFieldVisible(field, values, variables)).map(field => field.alias);
    });
    return computeVariables(definition.variables, definition.calculations, values, getHiddenFields, conditionsUseVariables(definition));
}
