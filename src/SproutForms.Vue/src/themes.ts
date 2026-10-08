import type { Component } from 'vue';

/**
 * The components a form is built from, the same parts the Razor views have. Replace one in a theme to change that part of
 * every form that uses the theme; it gets the form from useSproutFormContext().
 */
export interface FormComponents {
    // The form element with its progress, pages and actions, or the success message once submitted (Form.cshtml)
    Form: Component;
    // The steps of a form with more than one page, when the form shows its progress
    Progress: Component;
    // The rows and columns of a page or entry; gets `rows` (Rows.cshtml)
    Rows: Component;
    // A field's wrapper: label, errors and the field type's control; gets `field` (Field.cshtml)
    Field: Component;
    // One entry of a field group such as a repeater; gets `field` (the group), `path` (the group's), `index` and `removable`, and
    // emits `remove` (RepeaterEntry.cshtml). It calls provideEntryScope before rendering the entry's rows
    RepeaterEntry: Component;
    // The previous, next and submit buttons
    Actions: Component;
    // The errors that aren't about a field, such as a failed submit
    Errors: Component;
    // What shows instead of the form once it is submitted; gets `message` (HTML from the CMS) or `text`
    Success: Component;
}

export type FormComponentName = keyof FormComponents;

/**
 * A set of components. What a theme leaves out comes from the default theme, and what that leaves out from the built-in one.
 * Fields are keyed by field type alias, such as "text" or a custom type's alias, and get fieldControlProps.
 */
export interface SproutFormsTheme {
    components?: Partial<FormComponents>;
    fields?: Record<string, Component>;
}

/**
 * The theme a form uses unless it names another. Registering into it changes every form.
 */
export const defaultThemeName = 'default';

/**
 * Types a theme; it does nothing at runtime.
 */
export function defineTheme(theme: SproutFormsTheme): SproutFormsTheme {
    return theme;
}
