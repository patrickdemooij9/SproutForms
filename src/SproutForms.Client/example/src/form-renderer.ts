import {
    getEntries,
    getEntryPrefix,
    getEntryScope,
    getEntryTitle,
    getFieldByPath,
    getFields,
    getGroupFields,
    getNextPageIndex,
    getPageFields,
    getPreviousPageIndex,
    getVisiblePageIndexes,
    handleOutcome,
    isFieldGroup,
    isFieldOfType,
    isFieldRequired,
    isFieldVisible,
    isLastPage,
    registerOutcomeHandler,
    validateForm,
    type FormClientField,
    type FormClientModel,
    type FormClientRow,
    type FormErrors,
    type FormValues,
    type RepeaterFieldConfiguration,
    type SproutFormsClient,
    type SubmitResult
} from '@sproutforms/client';

export interface FormRendererOptions {
    client: SproutFormsClient;
    definition: FormClientModel;
    // Off to see what the server says about values the browser would have stopped
    skipClientValidation: () => boolean;
    // Lets the playground change what is sent, such as filling in the honeypot
    beforeSubmit?: (values: FormValues) => void;
    onSubmitted?: (result: SubmitResult) => void;
}

// Where a field's value lives: the form's values or a field group's entry, with the path its input and errors are keyed by
interface FieldScope {
    values: FormValues;
    pathPrefix: string;
    // What its conditions see: an entry's values over the form's
    conditionValues: () => FormValues;
}

// The alias of the form's field a path such as "people[0].email" belongs to
function toRootAlias(path: string): string {
    return path.split(/[.[]/)[0];
}

/**
 * Renders a headless form with plain DOM: one page at a time, conditions, validation in the browser and on the server, and the outcome.
 */
export function mountForm(root: HTMLElement, options: FormRendererOptions): void {
    const { client, definition } = options;
    const values: FormValues = {};
    let errors: FormErrors = {};
    let currentPage = 0;
    // The columns to show or hide as the values change, with the scope their field's conditions see
    let conditionalColumns: { column: HTMLElement; field: FormClientField; scope: FieldScope }[] = [];
    const formScope: FieldScope = { values, pathPrefix: '', conditionValues: () => values };

    // A real front-end would hand these to its router
    registerOutcomeHandler('message', outcome => showSuccess(String(outcome.data.message ?? definition.texts.submitSucceeded)));
    registerOutcomeHandler('redirect', outcome => showSuccess(`Your front-end would now go to ${outcome.data.url}.`));
    registerOutcomeHandler('redirectUmbracoPage', outcome => showSuccess(`Your front-end would now route to ${outcome.data.path} (content ${outcome.data.contentKey}).`));

    initValues(getFields(definition), values);

    // Hidden fields start with their default value, and field groups with their initial entries
    function initValues(fields: FormClientField[], target: FormValues) {
        for (const field of fields) {
            if (isFieldOfType(field, 'hidden')) target[field.alias] = field.configuration.defaultValue ?? '';
            if (isFieldOfType(field, 'repeater')) {
                target[field.alias] = Array.from({ length: field.configuration.initialItems }, () => createEntry(field));
            }
        }
    }

    function createEntry(field: FormClientField): FormValues {
        const entry: FormValues = {};
        initValues(getGroupFields(field), entry);
        return entry;
    }

    function render() {
        const form = element('form', { novalidate: '' });
        form.addEventListener('submit', event => {
            event.preventDefault();
            void onSubmit();
        });

        const guardFieldName = definition.submissionGuard?.alias === 'honeypot'
            ? String((definition.submissionGuard.settings as { fieldName?: string }).fieldName)
            : undefined;
        if (guardFieldName) {
            // Only a bot fills this in; its value goes to the guard with the rest of the values
            const honeypot = element('input', { type: 'text', name: guardFieldName, tabindex: '-1', autocomplete: 'off', 'aria-hidden': 'true', class: 'honeypot' });
            honeypot.addEventListener('input', () => { values[guardFieldName] = (honeypot as HTMLInputElement).value; });
            form.append(honeypot);
        }

        const globalErrors = Object.entries(errors).filter(([key]) => !getFieldByPath(definition, key));
        for (const [key, messages] of globalErrors) {
            form.append(element('p', { class: 'error', role: 'alert' }, `${messages.join(' ')} (${key})`));
        }

        const paged = definition.pages.length > 1;
        if (paged && definition.showProgress) {
            const progress = element('ol', { class: 'progress' });
            for (const index of getVisiblePageIndexes(definition, values, currentPage)) {
                progress.append(element('li', index === currentPage ? { 'aria-current': 'step' } : {}, definition.pages[index].progressLabel));
            }
            form.append(progress);
        }

        const page = definition.pages[currentPage];
        if (paged && page.title) form.append(element('h3', {}, page.title));

        conditionalColumns = [];
        form.append(...renderRows(page.rows, formScope));

        const actions = element('div', { class: 'actions' });
        if (paged && getPreviousPageIndex(definition, values, currentPage) !== undefined) {
            const previous = element('button', { type: 'button' }, page.previousLabel);
            previous.addEventListener('click', () => {
                currentPage = getPreviousPageIndex(definition, values, currentPage)!;
                errors = {};
                render();
            });
            actions.append(previous);
        }
        actions.append(element('button', { type: 'submit' }, isLastPage(definition, values, currentPage) ? definition.submitLabel : page.nextLabel));
        form.append(actions);

        root.replaceChildren(form);
    }

    function renderRows(rows: FormClientRow[], scope: FieldScope): HTMLElement[] {
        return rows.map(row => {
            const rowElement = element('div', { class: 'row' });
            for (const column of row.columns) {
                const field = column.field;
                const col = element('div', { class: 'col', style: `flex: ${column.width}` });
                col.append(renderField(field, scope));
                col.hidden = !isFieldVisible(field, scope.conditionValues());
                conditionalColumns.push({ column: col, field, scope });
                rowElement.append(col);
            }
            return rowElement;
        });
    }

    function renderField(field: FormClientField, scope: FieldScope): HTMLElement {
        const path = scope.pathPrefix + field.alias;
        const wrapper = element('div', { class: 'field' });
        const required = isFieldRequired(field, scope.conditionValues()) ? ' *' : '';
        const onInput = (value: unknown) => {
            scope.values[field.alias] = value;
            delete errors[path];
            updateConditions();
        };

        if (isFieldGroup(field)) {
            wrapper.append(renderRepeater(field, scope, path, required));
        } else if (isFieldOfType(field, 'radio')) {
            const fieldset = element('fieldset');
            fieldset.append(element('legend', {}, field.label + required));
            for (const option of field.configuration.options) {
                const input = element('input', { type: 'radio', name: path, value: option.value }) as HTMLInputElement;
                input.checked = scope.values[field.alias] === option.value;
                input.addEventListener('change', () => onInput(option.value));
                const label = element('label');
                label.append(input, ` ${option.label}`);
                fieldset.append(label);
            }
            wrapper.append(fieldset);
        } else if (isFieldOfType(field, 'checkbox')) {
            const input = element('input', { type: 'checkbox', id: path }) as HTMLInputElement;
            input.checked = scope.values[field.alias] === true;
            input.addEventListener('change', () => onInput(input.checked));
            const label = element('label');
            label.append(input, ` ${field.label}${required}`);
            wrapper.append(label);
        } else if (isFieldOfType(field, 'hidden')) {
            wrapper.append(element('small', { class: 'muted' }, `Hidden field ${path} = "${scope.values[field.alias]}"`));
        } else {
            wrapper.append(element('label', { for: path }, field.label + required));
            wrapper.append(renderInput(field, path, scope.values[field.alias], onInput));
        }

        const fieldErrors = errors[path];
        if (fieldErrors) {
            // A field group's own error, such as too few entries, isn't about the inputs of its entries
            if (!isFieldGroup(field)) wrapper.querySelectorAll('input, select, textarea').forEach(input => input.setAttribute('aria-invalid', 'true'));
            wrapper.append(element('div', { class: 'error', role: 'alert' }, fieldErrors.join(' ')));
        }
        return wrapper;
    }

    // Each entry's fields are keyed by their path, such as people[0].email, so the server's errors land on the entry the visitor sees
    function renderRepeater(field: FormClientField, scope: FieldScope, path: string, required: string): HTMLElement {
        const config = field.configuration as RepeaterFieldConfiguration;
        const entries = getEntries(scope.values[field.alias]);
        scope.values[field.alias] = entries;

        const fieldset = element('fieldset', { class: 'repeater' });
        fieldset.append(element('legend', {}, field.label + required));

        entries.forEach((entry, index) => {
            const entryElement = element('div', { class: 'repeater-entry' });
            const title = getEntryTitle(field, index);
            if (title) entryElement.append(element('h4', {}, title));

            const entryScope: FieldScope = {
                values: entry,
                pathPrefix: getEntryPrefix(path, index),
                conditionValues: () => getEntryScope(entry, scope.conditionValues())
            };
            entryElement.append(...renderRows(field.rows ?? [], entryScope));

            if (entries.length > (config.minItems ?? 0)) {
                const remove = element('button', { type: 'button', class: 'repeater-remove' }, config.removeLabel);
                remove.addEventListener('click', () => {
                    entries.splice(index, 1);
                    // The entries after it move up, so their errors no longer match
                    for (const key of Object.keys(errors)) {
                        if (key === path || key.startsWith(`${path}[`)) delete errors[key];
                    }
                    render();
                });
                entryElement.append(remove);
            }
            fieldset.append(entryElement);
        });

        if (config.maxItems === null || config.maxItems === undefined || entries.length < config.maxItems) {
            const add = element('button', { type: 'button', class: 'repeater-add' }, config.addLabel);
            add.addEventListener('click', () => {
                entries.push(createEntry(field));
                delete errors[path];
                render();
            });
            fieldset.append(add);
        }
        return fieldset;
    }

    function renderInput(field: FormClientField, path: string, current: unknown, onInput: (value: unknown) => void): HTMLElement {
        if (isFieldOfType(field, 'select')) {
            const select = element('select', { id: path }) as HTMLSelectElement;
            select.append(element('option', { value: '' }, ''));
            for (const option of field.configuration.options) {
                select.append(element('option', { value: option.value }, option.label));
            }
            select.value = String(current ?? '');
            select.addEventListener('change', () => onInput(select.value));
            return select;
        }
        if (isFieldOfType(field, 'textarea')) {
            const textarea = element('textarea', { id: path, rows: String(field.configuration.rows) }) as HTMLTextAreaElement;
            textarea.value = String(current ?? '');
            textarea.addEventListener('input', () => onInput(textarea.value));
            return textarea;
        }
        if (isFieldOfType(field, 'file')) {
            const accept = field.configuration.allowedExtensions?.join(',');
            const input = element('input', { id: path, type: 'file', ...(accept ? { accept } : {}) }) as HTMLInputElement;
            input.addEventListener('change', () => onInput(input.files?.[0]));
            return input;
        }

        const type = isFieldOfType(field, 'email') ? 'email' : isFieldOfType(field, 'date') ? 'date' : 'text';
        const placeholder = (field.configuration as { placeholder?: string | null } | null)?.placeholder;
        const input = element('input', { id: path, type, ...(placeholder ? { placeholder } : {}) }) as HTMLInputElement;
        input.value = String(current ?? '');
        input.addEventListener('input', () => onInput(input.value));
        return input;
    }

    // Shows and hides fields as their conditions change, without rendering the inputs again
    function updateConditions() {
        for (const { column, field, scope } of conditionalColumns) {
            column.hidden = !isFieldVisible(field, scope.conditionValues());
        }
    }

    async function onSubmit() {
        const onLastPage = isLastPage(definition, values, currentPage);

        errors = options.skipClientValidation() ? {} : await validateForm(definition, values, { pageIndex: currentPage });
        if (Object.keys(errors).length === 0 && !onLastPage) {
            errors = await client.validatePage(definition, currentPage, values);
        }
        if (Object.keys(errors).length > 0) return render();

        if (!onLastPage) {
            currentPage = getNextPageIndex(definition, values, currentPage)!;
            return render();
        }

        try {
            const submitted = { ...values };
            options.beforeSubmit?.(submitted);
            const result = await client.submit(definition, { values: submitted });
            options.onSubmitted?.(result);
            if (!result.ok) {
                errors = result.errors;
                // Go back to the first page with an error
                const aliasesWithError = new Set(Object.keys(errors).map(toRootAlias));
                const pageWithError = getVisiblePageIndexes(definition, values, currentPage)
                    .find(index => getPageFields(definition.pages[index]).some(field => aliasesWithError.has(field.alias)));
                if (pageWithError !== undefined) currentPage = pageWithError;
                return render();
            }
            if (!await handleOutcome(result.outcome, { definition })) {
                showSuccess(definition.texts.submitSucceeded);
            }
        } catch {
            errors = { form: [definition.texts.submitFailed] };
            render();
        }
    }

    function showSuccess(message: string) {
        // The message is written by an editor, but render it as text anyway
        root.replaceChildren(element('p', { class: 'success', role: 'status' }, message));
    }

    render();
}

export function element(tag: string, attributes: Record<string, string> = {}, text?: string): HTMLElement {
    const el = document.createElement(tag);
    for (const [name, value] of Object.entries(attributes)) el.setAttribute(name, value);
    if (text !== undefined) el.textContent = text;
    return el;
}
