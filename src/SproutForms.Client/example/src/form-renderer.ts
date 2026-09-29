import {
    getNextPageIndex,
    getField,
    getFields,
    getPageFields,
    getPreviousPageIndex,
    getVisiblePageIndexes,
    handleOutcome,
    isFieldOfType,
    isFieldRequired,
    isFieldVisible,
    isLastPage,
    registerOutcomeHandler,
    validateForm,
    type FormClientField,
    type FormClientModel,
    type FormErrors,
    type FormValues,
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

/**
 * Renders a headless form with plain DOM: one page at a time, conditions, validation in the browser and on the server, and the outcome.
 */
export function mountForm(root: HTMLElement, options: FormRendererOptions): void {
    const { client, definition } = options;
    const values: FormValues = {};
    let errors: FormErrors = {};
    let currentPage = 0;

    // A real front-end would hand these to its router
    registerOutcomeHandler('message', outcome => showSuccess(String(outcome.data.message ?? definition.texts.submitSucceeded)));
    registerOutcomeHandler('redirect', outcome => showSuccess(`Your front-end would now go to ${outcome.data.url}.`));
    registerOutcomeHandler('redirectUmbracoPage', outcome => showSuccess(`Your front-end would now route to ${outcome.data.path} (content ${outcome.data.contentKey}).`));

    for (const field of getFields(definition)) {
        if (isFieldOfType(field, 'hidden')) values[field.alias] = field.configuration.defaultValue ?? '';
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

        const globalErrors = Object.entries(errors).filter(([key]) => !getField(definition, key));
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

        for (const row of page.rows) {
            const rowElement = element('div', { class: 'row' });
            for (const column of row.columns) {
                const field = column.field;
                const col = element('div', { class: 'col', style: `flex: ${column.width}` });
                col.append(renderField(field));
                col.hidden = !isFieldVisible(field, values);
                col.dataset.field = field.alias;
                rowElement.append(col);
            }
            form.append(rowElement);
        }

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

    function renderField(field: FormClientField): HTMLElement {
        const wrapper = element('div', { class: 'field' });
        const required = isFieldRequired(field, values) ? ' *' : '';
        const onInput = (value: unknown) => {
            values[field.alias] = value;
            delete errors[field.alias];
            updateConditions();
        };

        if (isFieldOfType(field, 'radio')) {
            const fieldset = element('fieldset');
            fieldset.append(element('legend', {}, field.label + required));
            for (const option of field.configuration.options) {
                const input = element('input', { type: 'radio', name: field.alias, value: option.value }) as HTMLInputElement;
                input.checked = values[field.alias] === option.value;
                input.addEventListener('change', () => onInput(option.value));
                const label = element('label');
                label.append(input, ` ${option.label}`);
                fieldset.append(label);
            }
            wrapper.append(fieldset);
        } else if (isFieldOfType(field, 'checkbox')) {
            const input = element('input', { type: 'checkbox', id: field.alias }) as HTMLInputElement;
            input.checked = values[field.alias] === true;
            input.addEventListener('change', () => onInput(input.checked));
            const label = element('label');
            label.append(input, ` ${field.label}${required}`);
            wrapper.append(label);
        } else if (isFieldOfType(field, 'hidden')) {
            wrapper.append(element('small', { class: 'muted' }, `Hidden field ${field.alias} = "${values[field.alias]}"`));
        } else {
            wrapper.append(element('label', { for: field.alias }, field.label + required));
            wrapper.append(renderInput(field, onInput));
        }

        const fieldErrors = errors[field.alias];
        if (fieldErrors) {
            wrapper.querySelectorAll('input, select, textarea').forEach(input => input.setAttribute('aria-invalid', 'true'));
            wrapper.append(element('div', { class: 'error', role: 'alert' }, fieldErrors.join(' ')));
        }
        return wrapper;
    }

    function renderInput(field: FormClientField, onInput: (value: unknown) => void): HTMLElement {
        const current = values[field.alias];
        if (isFieldOfType(field, 'select')) {
            const select = element('select', { id: field.alias }) as HTMLSelectElement;
            select.append(element('option', { value: '' }, ''));
            for (const option of field.configuration.options) {
                select.append(element('option', { value: option.value }, option.label));
            }
            select.value = String(current ?? '');
            select.addEventListener('change', () => onInput(select.value));
            return select;
        }
        if (isFieldOfType(field, 'textarea')) {
            const textarea = element('textarea', { id: field.alias, rows: String(field.configuration.rows) }) as HTMLTextAreaElement;
            textarea.value = String(current ?? '');
            textarea.addEventListener('input', () => onInput(textarea.value));
            return textarea;
        }
        if (isFieldOfType(field, 'file')) {
            const accept = field.configuration.allowedExtensions?.join(',');
            const input = element('input', { id: field.alias, type: 'file', ...(accept ? { accept } : {}) }) as HTMLInputElement;
            input.addEventListener('change', () => onInput(input.files?.[0]));
            return input;
        }

        const type = isFieldOfType(field, 'email') ? 'email' : isFieldOfType(field, 'date') ? 'date' : 'text';
        const placeholder = (field.configuration as { placeholder?: string | null } | null)?.placeholder;
        const input = element('input', { id: field.alias, type, ...(placeholder ? { placeholder } : {}) }) as HTMLInputElement;
        input.value = String(current ?? '');
        input.addEventListener('input', () => onInput(input.value));
        return input;
    }

    // Shows and hides fields as their conditions change, without rendering the inputs again
    function updateConditions() {
        root.querySelectorAll<HTMLElement>('[data-field]').forEach(col => {
            const field = getField(definition, col.dataset.field!)!;
            col.hidden = !isFieldVisible(field, values);
        });
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
                const pageWithError = getVisiblePageIndexes(definition, values, currentPage)
                    .find(index => getPageFields(definition.pages[index]).some(field => errors[field.alias]));
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
