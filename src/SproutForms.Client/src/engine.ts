import type { FormClientField, FormClientModel, HeadlessOutcome } from './api/types.gen';
import { calculateVariables } from './calculations';
import type { SproutFormsClient, SubmitResult } from './client';
import { isFieldRequired, isFieldVisible } from './conditions';
import { globalSubmissionGuards, loadSubmissionGuard, type SubmissionGuardHandler } from './guards';
import { getFields } from './pages';
import type { Lookup } from './registry';
import type { FormErrors, FormValues, FormVariables } from './types';
import { globalValidators, validateForm, type Validator } from './validation';

/**
 * Where a form is: being filled in, submitting, or submitted.
 */
export type FormStatus = 'idle' | 'submitting' | 'submitted';

/**
 * A snapshot of a form. The engine replaces it on every change rather than changing it, so a renderer can compare snapshots.
 */
export interface FormState {
    readonly values: FormValues;
    // Worked out from the values, as the server does
    readonly variables: FormVariables;
    // Shown after a submit, from the browser's validation or the server's; a field's error goes once its value changes
    readonly errors: FormErrors;
    readonly status: FormStatus;
    // The outcome of the submit once the form is submitted; null when the server had none to give
    readonly outcome: HeadlessOutcome | null;
    // Why the last submit failed other than on validation, such as a network error; show definition.texts.submitFailed
    readonly submitError: unknown;
}

export interface FormEngineOptions {
    // Needed to submit; without it the engine only validates
    client?: SproutFormsClient;
    // Keyed by field alias, over the defaults of the form's hidden fields
    initialValues?: FormValues;
    // Where the validators and submission guard handlers come from; the global ones by default
    validators?: Lookup<Validator>;
    guards?: Lookup<SubmissionGuardHandler>;
    // Sent with the submission; the current page in a browser by default
    pageUrl?: string;
}

export type FormStateListener = (state: FormState) => void;

/**
 * The values the form starts with: the default of each hidden field, then the initial values.
 */
function getStartValues(definition: FormClientModel, initialValues: FormValues = {}): FormValues {
    const values: FormValues = {};
    for (const field of getFields(definition)) {
        const defaultValue = field.type === 'hidden' ? (field.configuration as { defaultValue?: string | null } | null | undefined)?.defaultValue : undefined;
        if (defaultValue !== null && defaultValue !== undefined) values[field.alias] = defaultValue;
    }
    return { ...values, ...initialValues };
}

/**
 * The errors that aren't about one of the form's fields, such as a submission guard that failed.
 */
export function getFormErrors(definition: FormClientModel, errors: FormErrors): string[] {
    const fieldAliases = new Set(getFields(definition).map(field => field.alias));
    return Object.entries(errors)
        .filter(([path]) => !fieldAliases.has(path.split(/[.[]/)[0]))
        .flatMap(([, messages]) => messages);
}

/**
 * Holds the state of one form and everything that changes it: the values, the variables worked out from them, validation and
 * the submit. It doesn't touch the DOM, so a renderer for any framework, or a server, can drive it. Create one per form on the
 * page, and on a server one per request.
 */
export function createFormEngine(definition: FormClientModel, options: FormEngineOptions = {}) {
    const validators = options.validators ?? globalValidators;
    const guards = options.guards ?? globalSubmissionGuards;
    const listeners = new Set<FormStateListener>();

    function createState(values: FormValues): FormState {
        return {
            values,
            variables: calculateVariables(definition, values),
            errors: {},
            status: 'idle',
            outcome: null,
            submitError: undefined
        };
    }

    let state = createState(getStartValues(definition, options.initialValues));

    function setState(next: Partial<FormState>): void {
        state = { ...state, ...next };
        for (const listener of listeners) listener(state);
    }

    function withoutError(errors: FormErrors, path: string): FormErrors {
        if (!(path in errors)) return errors;
        const { [path]: _removed, ...rest } = errors;
        return rest;
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

        /**
         * Sets a field's value by alias, works the variables out again and drops the field's error.
         */
        setValue(alias: string, value: unknown): void {
            const values = { ...state.values, [alias]: value };
            setState({
                values,
                variables: calculateVariables(definition, values),
                errors: withoutError(state.errors, alias)
            });
        },

        isFieldVisible(field: FormClientField): boolean {
            return isFieldVisible(field, state.values, state.variables);
        },

        isFieldRequired(field: FormClientField): boolean {
            return isFieldRequired(field, state.values, state.variables);
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
         * Validates the form and, when it is valid, submits it. Validation errors from the browser or the server end up in the
         * state's errors; any other failure in submitError, and is thrown.
         */
        async submit(): Promise<SubmitResult> {
            if (!options.client) throw new Error('The form engine needs a client to submit.');
            if (state.status === 'submitting') return { ok: false, errors: state.errors };

            const errors = await validateForm(definition, state.values, { validators });
            if (Object.keys(errors).length > 0) {
                setState({ errors });
                return { ok: false, errors };
            }

            setState({ status: 'submitting', errors: {}, submitError: undefined });
            try {
                const result = await options.client.submit(definition, { values: state.values, pageUrl: options.pageUrl, guards });
                if (result.ok) {
                    setState({ status: 'submitted', outcome: result.outcome });
                } else {
                    setState({ status: 'idle', errors: result.errors });
                }
                return result;
            } catch (error) {
                setState({ status: 'idle', submitError: error });
                throw error;
            }
        },

        /**
         * Starts over with the values the form started with.
         */
        reset(): void {
            state = createState(getStartValues(definition, options.initialValues));
            for (const listener of listeners) listener(state);
        }
    };
}

export type FormEngine = ReturnType<typeof createFormEngine>;
