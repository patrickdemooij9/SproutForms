import type { FormClientField, FormClientModel, FormClientPage, FormClientRow } from './api/types.gen';
import { calculateVariables } from './calculations';
import { isPageVisible } from './conditions';
import type { FormValues } from './types';

export type FieldGroup = FormClientField & { rows: FormClientRow[] };

function getRowFields(rows: FormClientRow[]): FormClientField[] {
    return rows.flatMap(row => row.columns).map(column => column.field);
}

/**
 * The fields on a page, in the order of its rows and columns. The fields in a field group's entries aren't included; see getGroupFields.
 */
export function getPageFields(page: FormClientPage): FormClientField[] {
    return getRowFields(page.rows);
}

/**
 * Every field of the form, page by page. Each field is in exactly one column. The fields in a field group's entries aren't included.
 */
export function getFields(definition: FormClientModel): FormClientField[] {
    return definition.pages.flatMap(getPageFields);
}

export function getField(definition: FormClientModel, alias: string): FormClientField | undefined {
    return getFields(definition).find(field => field.alias === alias);
}

/**
 * A field that holds a list of entries, such as a repeater. Its rows are the layout of one entry.
 */
export function isFieldGroup(field: FormClientField): field is FieldGroup {
    return Array.isArray(field.rows);
}

/**
 * The fields of one entry of a field group, in the order of its rows and columns.
 */
export function getGroupFields(field: FormClientField): FormClientField[] {
    return field.rows ? getRowFields(field.rows) : [];
}

/**
 * A field group's entries in a value; anything that isn't a list of entries counts as none.
 */
export function getEntries(value: unknown): FormValues[] {
    if (!Array.isArray(value)) return [];
    return value.map(entry => entry !== null && typeof entry === 'object' && !Array.isArray(entry) ? entry as FormValues : {});
}

/**
 * What the paths of the fields in an entry start with, the same as on the server: getEntryPrefix("people", 0) + "email" is
 * "people[0].email". Errors, and the uploads of a multipart submit, are keyed by these paths.
 */
export function getEntryPrefix(groupPath: string, index: number): string {
    return `${groupPath}[${index}].`;
}

/**
 * The heading of an entry from the group's itemTitle, where {n} is its 1-based number; undefined when it has none.
 */
export function getEntryTitle(field: FormClientField, index: number): string | undefined {
    const itemTitle = (field.configuration as { itemTitle?: string | null } | null | undefined)?.itemTitle;
    return itemTitle && itemTitle.trim() ? itemTitle.replace(/\{n\}/g, String(index + 1)) : undefined;
}

/**
 * The field a path leads to, such as "email" or "people[0].email", through the field groups on the way. Undefined when the path
 * isn't a field, such as the "submissionGuard" error key.
 */
export function getFieldByPath(definition: FormClientModel, path: string): FormClientField | undefined {
    const segments = path.split('.');
    let fields = getFields(definition);
    for (const segment of segments.slice(0, -1)) {
        const match = /^([^.[\]]+)\[\d+\]$/.exec(segment);
        const group = match ? fields.find(field => field.alias === match[1]) : undefined;
        if (!group || !isFieldGroup(group)) return undefined;
        fields = getGroupFields(group);
    }
    return fields.find(field => field.alias === segments[segments.length - 1]);
}

/**
 * The pages the visitor goes through with the values entered so far, and the variables worked out from them. The current page
 * always counts: its conditions only depend on earlier pages.
 */
export function getVisiblePageIndexes(definition: FormClientModel, values: FormValues, currentIndex?: number): number[] {
    const variables = calculateVariables(definition, values);
    return definition.pages
        .filter(page => page.index === currentIndex || isPageVisible(page, values, variables))
        .map(page => page.index);
}

export function getNextPageIndex(definition: FormClientModel, values: FormValues, currentIndex: number): number | undefined {
    const visible = getVisiblePageIndexes(definition, values, currentIndex);
    return visible[visible.indexOf(currentIndex) + 1];
}

export function getPreviousPageIndex(definition: FormClientModel, values: FormValues, currentIndex: number): number | undefined {
    const visible = getVisiblePageIndexes(definition, values, currentIndex);
    return visible[visible.indexOf(currentIndex) - 1];
}

/**
 * On the last page the visitor goes through, the form is submitted instead of going to the next page.
 */
export function isLastPage(definition: FormClientModel, values: FormValues, currentIndex: number): boolean {
    return getNextPageIndex(definition, values, currentIndex) === undefined;
}
