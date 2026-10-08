import type { FormClientField, FormClientModel, HeadlessOutcome } from './api/types.gen';
import { calculateVariables } from './calculations';
import type { SproutFormsClient, SubmitResult } from './client';
import { isFieldRequired, isFieldVisible } from './conditions';
import { globalSubmissionGuards, loadSubmissionGuard, type SubmissionGuardHandler } from './guards';
import { getFieldByPath, getFields, getGroupFields, getNextPageIndex, getPageFields, getPreviousPageIndex, getVisiblePageIndexes, isFieldGroup, isLastPage } from './pages';
import { getValueAtPath, setValueAtPath } from './paths';
import type { Lookup } from './registry';
import type { FormErrors, FormValues, FormVariables } from './types';
import { globalValidators, validateForm, type Validator } from './validation';

/**
 * Where a form is: being filled in, checking a page with the server before going to the next one, submitting, or submitted.
 */
export type FormStatus = 'idle' | 'validating' | 'submitting' | 'submitted';

/**
 * A snapshot of a form. The engine replaces it on every change rather than changing it, so a renderer can compare snapshots.
 */
export interface FormState {
    // Keyed by field alias; a field group's value is a list of entries, keyed by their fields' aliases
    readonly values: FormValues;
    // Worked out from the values, as the server does
    readonly variables: FormVariables;
    // Keyed by field path, such as "people[0].email". Shown after going to the next page or submitting, from the browser's
    // validation or the server's; a field's error goes once its value changes
    readonly errors: FormErrors;
    // The page the visitor is on: its index in the definition's pages
    readonly pageIndex: number;
    readonly status: FormStatus;
    // The outcome of the submit once the form is submitted; null when the server had none to give
    readonly outcome: HeadlessOutcome | null;
    // Why the last request failed other than on validation, such as a network error; show definition.texts.submitFailed
    readonly submitError: unknown;
}

export interface FormEngineOptions {
    // Needed to submit, and to check a page with the server before going to the next one
    client?: SproutFormsClient;
    // Keyed by field alias, over the defaults of the form's hidden fields and the first entries of its field groups
    initialValues?: FormValues;
    // Where the validators and submission guard handlers come from; the global ones by default
    validators?: Lookup<Validator>;
    guards?: Lookup<SubmissionGuardHandler>;
    // Sent with the submission; the current page in a browser by default
    pageUrl?: string;
}

export type FormStateListener = (state: FormState) => void;

function getHiddenDefault(field: FormClientField): string | undefined {
    if (field.type !== 'hidden') return undefined;
    return (field.configuration as { defaultValue?: string | null } | null | undefined)?.defaultValue ?? undefined;
}

/**
 * The values a set of fields starts with: each hidden field's default, and a field group's first entries (initialItems).
 */
function getStartValuesOf(fields: FormClientField[]): FormValues {
    const values: FormValues = {};
    for (const field of fields) {
        const defaultValue = getHiddenDefault(field);
        if (defaultValue !== undefined) values[field.alias] = defaultValue;
        if (isFieldGroup(field)) {
            const initialItems = (field.configuration as { initialItems?: number } | null | undefined)?.initialItems ?? 0;
            values[field.alias] = Array.from({ length: initialItems }, () => createEntry(field));
        }
    }
    return values;
}

/**
 * A new entry of a field group, with the defaults of its hidden fields.
 */
function createEntry(group: FormClientField): FormValues {
    return getStartValuesOf(getGroupFields(group));
}

/**
 * The errors that aren't about one of the form's fields, such as a submission guard that failed.
 */
export function getFormErrors(definition: FormClientModel, errors: FormErrors): string[] {
    const fieldAliases = new Set(getFields(definition).map(field => field.alias));
    return Object.entries(errors)
        .filter(([path]) => !fieldAliases.has(getRootAlias(path)))
        .flatMap(([, messages]) => messages);
}

// The alias of the form's own field a path is in: "people" for "people[0].email"
function getRootAlias(path: string): string {
    return path.split(/[.[]/)[0];
}

/**
 * Holds the state of one form and everything that changes it: the values, the variables worked out from them, the page the
 * visitor is on, the entries of field groups, validation and the submit. It doesn't touch the DOM, so a renderer for any
 * framework, or a server, can drive it. Create one per form on the page, and on a server one per request.
 */
export function createFormEngine(definition: FormClientModel, options: FormEngineOptions = {}) {
    const validators = options.validators ?? globalValidators;
    const guards = options.guards ?? globalSubmissionGuards;
    const listeners = new Set<FormStateListener>();

    function createState(): FormState {
        const values = { ...getStartValuesOf(getFields(definition)), ...options.initialValues };
        return {
            values,
            variables: calculateVariables(definition, values),
            errors: {},
            pageIndex: getVisiblePageIndexes(definition, values)[0] ?? 0,
            status: 'idle',
            outcome: null,
            submitError: undefined
        };
    }

    let state = createState();

    function setState(next: Partial<FormState>): void {
        state = { ...state, ...next };
        for (const listener of listeners) listener(state);
    }

    function setValues(values: FormValues, errors: FormErrors): void {
        setState({ values, variables: calculateVariables(definition, values), errors });
    }

    // The errors without those of the path, and of everything in it when it is a field group
    function withoutErrors(path: string, includingEntries = false): FormErrors {
        return Object.fromEntries(Object.entries(state.errors).filter(([key]) =>
            key !== path && !(includingEntries && key.startsWith(`${path}[`))));
    }

    // The page with the first error that is about a field, so the visitor sees it
    function getPageWithError(errors: FormErrors): number | undefined {
        const aliases = new Set(Object.keys(errors).map(getRootAlias));
        return definition.pages.find(page => getPageFields(page).some(field => aliases.has(field.alias)))?.index;
    }

    function hasErrors(errors: FormErrors): boolean {
        return Object.keys(errors).length > 0;
    }

    return {
        definition,

        getState(): FormState {
            return state;
        },

        /**
         * Calls the listener with every new state. Returns the function that stops it.
         */
        subscribe(listener: FormStateListener): () => void {
            listeners.add(listener);
            return () => listeners.delete(listener);
        },

        getValue(path: string): unknown {
            return getValueAtPath(state.values, path);
        },

        /**
         * Sets a field's value by its path, such as "email" or "people[0].email", works the variables out again and drops the
         * field's error.
         */
        setValue(path: string, value: unknown): void {
            setValues(setValueAtPath(state.values, path, value), withoutErrors(path));
        },

        /**
         * Adds an entry to the field group at the path, such as "people", with the defaults of its hidden fields.
         */
        addEntry(path: string): void {
            const group = getFieldByPath(definition, path);
            if (!group || !isFieldGroup(group)) return;

            const entries = getValueAtPath(state.values, path);
            const next = [...(Array.isArray(entries) ? entries : []), createEntry(group)];
            setValues(setValueAtPath(state.values, path, next), withoutErrors(path));
        },

        /**
         * Removes an entry from the field group at the path. The entries after it move up, so their errors, which are keyed by
         * index, go too.
         */
        removeEntry(path: string, index: number): void {
            const entries = getValueAtPath(state.values, path);
            if (!Array.isArray(entries)) return;

            const next = entries.filter((_, entryIndex) => entryIndex !== index);
            setValues(setValueAtPath(state.values, path, next), withoutErrors(path, true));
        },

        /**
         * Whether a field shows. For a field in a field group's entry, pass the entry's scope (see getEntryScope).
         */
        isFieldVisible(field: FormClientField, scope: FormValues = state.values): boolean {
            return isFieldVisible(field, scope, state.variables);
        },

        isFieldRequired(field: FormClientField, scope: FormValues = state.values): boolean {
            return isFieldRequired(field, scope, state.variables);
        },

        getErrors(path: string): string[] {
            return state.errors[path] ?? [];
        },

        /**
         * The errors that aren't about a field, such as a submission guard that failed.
         */
        getFormErrors(): string[] {
            return getFormErrors(definition, state.errors);
        },

        /**
         * The pages the visitor goes through with the values so far, by index.
         */
        getVisiblePageIndexes(): number[] {
            return getVisiblePageIndexes(definition, state.values, state.pageIndex);
        },

        /**
         * On the last page the visitor goes through, the form is submitted instead of going to the next page.
         */
        isLastPage(): boolean {
            return isLastPage(definition, state.values, state.pageIndex);
        },

        /**
         * Validates the current page in the browser, then with the server's rules when there is a client, and goes to the next
         * page the visitor goes through when it is valid. Returns whether it went.
         */
        async next(): Promise<boolean> {
            if (state.status !== 'idle') return false;
            const pageIndex = state.pageIndex;

            const errors = await validateForm(definition, state.values, { pageIndex, validators });
            if (hasErrors(errors)) {
                setState({ errors });
                return false;
            }

            if (options.client) {
                setState({ status: 'validating', errors: {}, submitError: undefined });
                try {
                    const serverErrors = await options.client.validatePage(definition, pageIndex, state.values);
                    if (hasErrors(serverErrors)) {
                        setState({ status: 'idle', errors: serverErrors });
                        return false;
                    }
                } catch (error) {
                    setState({ status: 'idle', submitError: error });
                    throw error;
                }
            }

            const next = getNextPageIndex(definition, state.values, pageIndex);
            setState({ status: 'idle', errors: {}, pageIndex: next ?? pageIndex });
            return next !== undefined;
        },

        /**
         * Goes back to the previous page the visitor went through, without validating.
         */
        previous(): boolean {
            if (state.status !== 'idle') return false;
            const previous = getPreviousPageIndex(definition, state.values, state.pageIndex);
            if (previous === undefined) return false;

            setState({ pageIndex: previous });
            return true;
        },

        /**
         * Validates the fields the visitor can see and shows their errors. Returns them; none means the form is valid.
         */
        async validate(): Promise<FormErrors> {
            const errors = await validateForm(definition, state.values, { validators });
            setState({ errors });
            return errors;
        },

        /**
         * Loads what the form's submission guard needs up front, such as the reCAPTCHA script. Only call it in a browser.
         */
        loadSubmissionGuard(): Promise<void> {
            return loadSubmissionGuard(definition, guards);
        },

        /**
         * Validates the whole form and, when it is valid, submits it. Validation errors from the browser or the server end up in
         * the state's errors, on the first page that has one; any other failure in submitError, and is thrown.
         */
        async submit(): Promise<SubmitResult> {
            if (!options.client) throw new Error('The form engine needs a client to submit.');
            if (state.status !== 'idle') return { ok: false, errors: state.errors };

            const errors = await validateForm(definition, state.values, { validators });
            if (hasErrors(errors)) {
                setState({ errors, pageIndex: getPageWithError(errors) ?? state.pageIndex });
                return { ok: false, errors };
            }

            setState({ status: 'submitting', errors: {}, submitError: undefined });
            try {
                const result = await options.client.submit(definition, { values: state.values, pageUrl: options.pageUrl, guards });
                if (result.ok) {
                    setState({ status: 'submitted', outcome: result.outcome });
                } else {
                    setState({ status: 'idle', errors: result.errors, pageIndex: getPageWithError(result.errors) ?? state.pageIndex });
                }
                return result;
            } catch (error) {
                setState({ status: 'idle', submitError: error });
                throw error;
            }
        },

        /**
         * Starts over with the values the form started with, on its first page.
         */
        reset(): void {
            state = createState();
            for (const listener of listeners) listener(state);
        }
    };
}

export type FormEngine = ReturnType<typeof createFormEngine>;
