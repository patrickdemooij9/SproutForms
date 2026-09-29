import type { FormClientField, FormClientModel, FormClientPage } from './api/types.gen';
import { isPageVisible } from './conditions';
import type { FormValues } from './types';

/**
 * The fields on a page, in the order of its rows and columns.
 */
export function getPageFields(page: FormClientPage): FormClientField[] {
    return page.rows.flatMap(row => row.columns).map(column => column.field);
}

/**
 * Every field of the form, page by page. Each field is in exactly one column.
 */
export function getFields(definition: FormClientModel): FormClientField[] {
    return definition.pages.flatMap(getPageFields);
}

export function getField(definition: FormClientModel, alias: string): FormClientField | undefined {
    return getFields(definition).find(field => field.alias === alias);
}

/**
 * The pages the visitor goes through with the values entered so far. The current page always counts: its conditions only
 * depend on earlier pages.
 */
export function getVisiblePageIndexes(definition: FormClientModel, values: FormValues, currentIndex?: number): number[] {
    return definition.pages
        .filter(page => page.index === currentIndex || isPageVisible(page, values))
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
