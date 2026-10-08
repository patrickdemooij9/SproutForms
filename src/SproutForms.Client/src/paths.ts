import { getEntryScope } from './conditions';
import type { FormValues } from './types';

interface PathSegment {
    alias: string;
    // The entry of a field group, for every segment but the last
    index?: number;
}

/**
 * A field's path as errors and uploads are keyed, such as "email" or "people[0].email", as segments.
 */
function parsePath(path: string): PathSegment[] {
    return path.split('.').map(segment => {
        const match = /^([^[\]]+)\[(\d+)\]$/.exec(segment);
        return match ? { alias: match[1], index: Number(match[2]) } : { alias: segment };
    });
}

function asEntry(value: unknown): FormValues {
    return value !== null && typeof value === 'object' && !Array.isArray(value) ? value as FormValues : {};
}

/**
 * The value of a field by its path, also in a field group's entry: getValueAtPath(values, "people[0].email").
 */
export function getValueAtPath(values: FormValues, path: string): unknown {
    let current: unknown = values;
    for (const segment of parsePath(path)) {
        current = asEntry(current)[segment.alias];
        if (segment.index !== undefined) current = Array.isArray(current) ? current[segment.index] : undefined;
    }
    return current;
}

/**
 * A copy of the values with the field at the path set, copying the entries on the way rather than changing them.
 */
export function setValueAtPath(values: FormValues, path: string, value: unknown): FormValues {
    const [segment, ...rest] = parsePath(path);
    if (rest.length === 0 || segment.index === undefined) {
        return { ...values, [segment.alias]: value };
    }

    const entries = Array.isArray(values[segment.alias]) ? [...values[segment.alias] as unknown[]] : [];
    const restPath = path.slice(path.indexOf('.') + 1);
    entries[segment.index] = setValueAtPath(asEntry(entries[segment.index]), restPath, value);
    return { ...values, [segment.alias]: entries };
}

/**
 * The entries a path goes through, as group paths and indexes: [["people", 0]] for "people[0].email".
 */
export function getEntryPaths(path: string): [groupPath: string, index: number][] {
    const result: [string, number][] = [];
    const pattern = /\[(\d+)\]/g;
    let match: RegExpExecArray | null;
    while ((match = pattern.exec(path)) !== null) {
        result.push([path.slice(0, match.index), Number(match[1])]);
    }
    return result;
}

/**
 * The values the conditions of the field at a path see: in a field group's entry, the entry's own values over the form's (see
 * getEntryScope); otherwise the form's.
 */
export function getPathScope(values: FormValues, path: string): FormValues {
    let scope = values;
    for (const [groupPath, index] of getEntryPaths(path)) {
        scope = getEntryScope(asEntry(getValueAtPath(values, `${groupPath}[${index}]`)), scope);
    }
    return scope;
}
