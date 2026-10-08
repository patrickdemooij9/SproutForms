// The browser side of the Razor forms: the form engine of @sproutforms/client, the same one headless front-ends use, driving the
// markup the Razor views rendered. The engine holds the values, conditions, calculations, validation, pages and submit; this file
// feeds it what the visitor enters and shows its state with data-sf-* attributes.
import {
    createFormEngine,
    getEntryTitle,
    getFieldByPath,
    getFormErrors,
    getPathScope,
    getSubmissionGuardValues,
    getVisiblePageIndexes,
    globalOutcomeHandlers,
    handleOutcome,
    isFieldRequired,
    isFieldVisible,
    Registry,
    registerSubmissionGuard,
    registerValidator,
    setValueAtPath,
    type FormClientModel,
    type FormEngine,
    type FormErrors,
    type FormState,
    type FormTransport,
    type FormValues,
    type OutcomeContext,
    type OutcomeHandler,
    type RepeaterFieldConfiguration,
    type SubmitResult
} from "../../../SproutForms.Client/src/index";

/**
 * What an outcome handler of a Razor form can do on top of the engine's context.
 */
export interface RazorOutcomeContext extends OutcomeContext {
    form: HTMLFormElement;
    // Replaces the form with a success message, as HTML
    showMessage(html: string): void;
    navigate(url: string): void;
}

export type RazorOutcomeHandler = OutcomeHandler<RazorOutcomeContext>;

interface PageChangeDetail {
    index: number;
    previousIndex: number;
}

// The response of the Razor endpoint, /api/forms/{id}
interface RazorSubmitResponse {
    success: boolean;
    errors?: FormErrors;
    outcomeType?: string | null;
    outcomeData?: Record<string, unknown> | null;
}

declare global {
    interface Window {
        SproutForms: {
            // The validator for a rule type, such as one a custom field type returns from GetValidationRules
            registerValidator: typeof registerValidator;
            // The browser side of a submission guard you registered on the server
            registerSubmissionGuard: typeof registerSubmissionGuard;
            // Shows the outcome of a submit, by outcome type
            registerOutcomeHandler(type: string, handler: RazorOutcomeHandler): void;
            // The engine of a form on the page, to read its state or drive it from your own script
            getEngine(form: HTMLFormElement): FormEngine | undefined;
        };
    }
}

const builtInOutcomeHandlers: Record<string, RazorOutcomeHandler> = {
    // Editors write the message in the CMS; never put submitted values in it
    message: (outcome, context) => {
        if (typeof outcome.data.message === "string" && outcome.data.message) context.showMessage(outcome.data.message);
        else context.showMessage(escapeHtml(context.definition.texts.submitSucceeded));
    },
    redirect: (outcome, context) => {
        if (typeof outcome.data.url === "string" && outcome.data.url) context.navigate(outcome.data.url);
    },
    redirectUmbracoPage: (outcome, context) => {
        if (typeof outcome.data.url === "string" && outcome.data.url) context.navigate(outcome.data.url);
    }
};

// Those registered on the page, then the global ones, then the built-in ones
const outcomeHandlers = new Registry<RazorOutcomeHandler>({
    get: type => globalOutcomeHandlers.get(type) ?? builtInOutcomeHandlers[type]
});

const engines = new WeakMap<HTMLFormElement, FormEngine>();

window.SproutForms = {
    registerValidator,
    registerSubmissionGuard,
    registerOutcomeHandler: (type, handler) => outcomeHandlers.register(type, handler),
    getEngine: form => engines.get(form)
};

document.addEventListener("DOMContentLoaded", () => {
    document.querySelectorAll<HTMLFormElement>("form[data-form-ajax]").forEach(initForm);
});

type ValueElement = HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement;

const antiforgeryName = "__RequestVerificationToken";

function initForm(form: HTMLFormElement) {
    const definitionElement = form.querySelector("script[data-sf-definition]");
    if (!definitionElement?.textContent) return;
    const definition = JSON.parse(definitionElement.textContent) as FormClientModel;

    const pageUrlInput = form.querySelector<HTMLInputElement>("[data-sf-page-url]");
    const engine = createFormEngine(definition, {
        transport: createRazorTransport(form, pageUrlInput?.name),
        // What the server rendered: the values and errors of a post without JavaScript, or the defaults
        initialValues: readValues(form, pageUrlInput?.name),
        initialErrors: readRenderedErrors(form),
        pageUrl: window.location.href
    });
    engines.set(form, engine);

    const view = createView(form, engine);
    engine.subscribe(state => view.render(state));
    view.render(engine.getState());

    form.addEventListener("input", event => updateValue(event.target));
    form.addEventListener("change", event => updateValue(event.target));

    // Leaving a field validates it, so its error shows before the visitor moves on
    form.addEventListener("focusout", event => {
        const field = getFieldOfInput(event.target);
        if (field?.dataset.sfFieldId) void engine.validateField(field.dataset.sfFieldId);
    });

    form.addEventListener("click", event => {
        const target = event.target as Element;
        if (target.closest("[data-sf-previous]")) {
            engine.previous();
        } else if (target.closest("[data-sf-next]")) {
            void next();
        } else if (target.closest("[data-sf-repeater-add]")) {
            view.addEntry(target.closest<HTMLElement>("[data-sf-repeater]")!);
        } else if (target.closest("[data-sf-repeater-remove]")) {
            view.removeEntry(target.closest<HTMLElement>("[data-sf-repeater-entry]")!);
        }
    });

    form.addEventListener("submit", event => {
        event.preventDefault();
        // Enter in a field submits the form, which on any page but the last means going to the next page
        void (engine.isLastPage() ? submit() : next());
    });

    engine.loadSubmissionGuard().catch(error => console.error("SproutForms: the submission guard could not load", error));

    function updateValue(target: EventTarget | null) {
        if (!isValueElement(target) || !target.name || isIgnored(form, target, pageUrlInput?.name)) return;
        if (target instanceof HTMLInputElement && target.type === "radio" && !target.checked) return;
        engine.setValue(target.name, readValue(target));
    }

    async function next() {
        try {
            if (!await engine.next()) view.focusFirstError();
        } catch (error) {
            console.error("SproutForms: the page could not be checked", error);
        }
    }

    async function submit() {
        let result: SubmitResult;
        try {
            result = await engine.submit();
        } catch (error) {
            console.error("SproutForms: the form could not be submitted", error);
            return;
        }

        if (!result.ok) {
            view.focusFirstError();
            return;
        }

        form.dispatchEvent(new CustomEvent("sproutforms:submitted", { bubbles: true, detail: { outcome: result.outcome } }));
        const context: RazorOutcomeContext = {
            definition,
            form,
            showMessage: html => view.showSuccess(html),
            navigate: url => { window.location.href = url; }
        };
        // The submission was saved even when nothing handles its outcome, so confirm it, or the visitor might submit it again
        if (!await handleOutcome(result.outcome, context, outcomeHandlers)) {
            view.showSuccess(escapeHtml(definition.texts.submitSucceeded));
        }
    }
}

// Values

function isValueElement(target: EventTarget | null): target is ValueElement {
    return target instanceof HTMLInputElement || target instanceof HTMLSelectElement || target instanceof HTMLTextAreaElement;
}

// The antiforgery token and the page URL are the transport's; the hidden "false" after a checkbox is only for posts without JavaScript
function isIgnored(form: HTMLFormElement, element: ValueElement, pageUrlName: string | undefined): boolean {
    if (element.name === antiforgeryName || element.name === pageUrlName) return true;
    return element instanceof HTMLInputElement && element.type === "hidden"
        && form.querySelector(`input[type=checkbox][name="${CSS.escape(element.name)}"]`) !== null;
}

// A checkbox's value is whether it's ticked, an upload's the file the visitor chose
function readValue(element: ValueElement): unknown {
    if (element instanceof HTMLInputElement) {
        if (element.type === "checkbox") return element.checked;
        if (element.type === "file") return element.files?.[0];
    }
    return element.value;
}

// The values of every named input, by its name: the field's path, such as "people[0].email". Inputs that aren't fields, such as the
// honeypot, end up in the values too, so the submission guard finds them
function readValues(form: HTMLFormElement, pageUrlName: string | undefined): FormValues {
    let values: FormValues = {};
    for (const element of Array.from(form.elements)) {
        if (!isValueElement(element) || !element.name || isIgnored(form, element, pageUrlName)) continue;
        if (element instanceof HTMLInputElement && element.type === "radio" && !element.checked) continue;
        values = setValueAtPath(values, element.name, readValue(element));
    }
    return values;
}

// The errors the server rendered after a post without JavaScript
function readRenderedErrors(form: HTMLFormElement): FormErrors {
    const errors: FormErrors = {};
    form.querySelectorAll<HTMLElement>("[data-sf-field-id]").forEach(field => {
        const messages = getOwnElements(field, "[data-sf-error]").flatMap(error => {
            const lines = Array.from(error.children).map(child => child.textContent?.trim() ?? "");
            return (lines.length > 0 ? lines : [error.textContent?.trim() ?? ""]).filter(Boolean);
        });
        if (messages.length > 0) errors[field.dataset.sfFieldId!] = messages;
    });
    return errors;
}

// Transport: the Razor endpoint, with the antiforgery token, as form fields named by path

function createRazorTransport(form: HTMLFormElement, pageUrlName: string | undefined): FormTransport {
    function toFormData(values: FormValues, withFiles: boolean): FormData {
        const data = new FormData();
        appendValues(data, values, "", withFiles);
        const token = form.querySelector<HTMLInputElement>(`input[name="${antiforgeryName}"]`)?.value;
        if (token) data.set(antiforgeryName, token);
        return data;
    }

    async function post(url: string, data: FormData): Promise<{ response: Response; result: RazorSubmitResponse }> {
        const response = await fetch(url, { method: "POST", headers: { "X-Requested-With": "XMLHttpRequest" }, body: data });
        const result = await response.json().catch(() => ({ success: false })) as RazorSubmitResponse;
        return { response, result };
    }

    return {
        async validatePage(_definition, pageIndex, values) {
            // Uploads are checked when the form is submitted
            const { response, result } = await post(`${form.action}/pages/${pageIndex}/validate`, toFormData(values, false));
            if (response.status === 400) return result.errors ?? {};
            if (!response.ok) throw new Error(`SproutForms answered ${response.status}`);
            return {};
        },

        async submit(definition, { values, pageUrl, guard, guards }) {
            const data = toFormData(values, true);
            const guardValues = { ...await getSubmissionGuardValues(definition, values, guards), ...guard };
            for (const [name, value] of Object.entries(guardValues)) data.set(name, value);
            if (pageUrlName && pageUrl) data.set(pageUrlName, pageUrl);

            const { response, result } = await post(form.action, data);
            if (response.status === 400) return { ok: false, errors: result.errors ?? {} };
            if (!response.ok) throw new Error(`SproutForms answered ${response.status}`);
            return { ok: true, outcome: result.outcomeType ? { type: result.outcomeType, data: result.outcomeData ?? {} } : null };
        }
    };
}

// A field group's entries as "people[0].email", booleans as the "true" and "false" a checkbox posts
function appendValues(data: FormData, values: FormValues, prefix: string, withFiles: boolean) {
    for (const [alias, value] of Object.entries(values)) {
        const path = prefix + alias;
        if (value instanceof Blob) {
            if (withFiles) data.append(path, value, value instanceof File ? value.name : path);
        } else if (Array.isArray(value)) {
            value.forEach((entry, index) => appendValues(data, (entry ?? {}) as FormValues, `${path}[${index}].`, withFiles));
        } else if (value !== null && value !== undefined) {
            data.append(path, String(value));
        }
    }
}

// View: the engine's state as attributes on the rendered markup

function createView(form: HTMLFormElement, engine: FormEngine) {
    const definition = engine.definition;
    const isPaged = form.hasAttribute("data-sf-paged");
    const renderedErrors = new WeakMap<Element, string>();
    let currentPage: number | undefined;

    function render(state: FormState) {
        form.toggleAttribute("aria-busy", state.status === "submitting" || state.status === "validating");
        renderFields(state);
        renderRepeaters();
        renderGlobalErrors(state);
        if (isPaged) renderPages(state);
    }

    function renderFields(state: FormState) {
        form.querySelectorAll<HTMLElement>("[data-sf-field-id]").forEach(wrapper => {
            const path = wrapper.dataset.sfFieldId!;
            const field = getFieldByPath(definition, path);
            if (!field) return;

            const scope = getPathScope(state.values, path);
            const visible = isFieldVisible(field, scope, state.variables);
            setHidden(wrapper, !visible);
            const column = wrapper.closest<HTMLElement>("[data-sf-col]");
            if (column) setHidden(column, !visible);

            const required = visible && isFieldRequired(field, scope, state.variables);
            getOwnElements(wrapper, "input, select, textarea").forEach(input => {
                if (required) input.setAttribute("aria-required", "true");
                else input.removeAttribute("aria-required");
            });

            renderFieldErrors(wrapper, path, state.errors[path] ?? []);
        });
    }

    // Only touched when the errors change, so a theme's own error markup stays until then
    function renderFieldErrors(wrapper: HTMLElement, path: string, messages: string[]) {
        const key = messages.join("\n");
        if ((renderedErrors.get(wrapper) ?? readRenderedKey(wrapper)) === key) return;
        renderedErrors.set(wrapper, key);

        getOwnElements(wrapper, "input, select, textarea").forEach(input => input.setAttribute("aria-invalid", String(messages.length > 0)));
        getOwnElements(wrapper, "[data-sf-error]").forEach(error => error.remove());
        if (messages.length === 0) return;

        const error = createErrorElement();
        error.id = `${path}-error`;
        error.setAttribute("role", "alert");
        for (const message of messages) {
            const line = document.createElement("div");
            line.textContent = message;
            error.appendChild(line);
        }
        wrapper.appendChild(error);
    }

    // The errors the server rendered, the first time
    function readRenderedKey(wrapper: HTMLElement): string {
        return getOwnElements(wrapper, "[data-sf-error]").flatMap(error => {
            const lines = Array.from(error.children).map(child => child.textContent?.trim() ?? "");
            return (lines.length > 0 ? lines : [error.textContent?.trim() ?? ""]).filter(Boolean);
        }).join("\n");
    }

    function renderGlobalErrors(state: FormState) {
        const messages = getFormErrors(definition, state.errors);
        if (state.submitError) messages.unshift(definition.texts.submitFailed);

        let container = form.querySelector<HTMLElement>("[data-sf-global-errors]");
        if (messages.length === 0) {
            container?.remove();
            return;
        }
        if (!container) {
            container = document.createElement("div");
            container.className = "form-global-errors";
            container.setAttribute("data-sf-global-errors", "");
            container.setAttribute("role", "alert");
            form.prepend(container);
        }
        container.replaceChildren(...messages.map(message => {
            const error = createErrorElement();
            error.textContent = message;
            return error;
        }));
    }

    // Numbers the entries in order, titles them, and shows the add and remove buttons the minimum and maximum allow
    function renderRepeaters() {
        form.querySelectorAll<HTMLElement>("[data-sf-repeater]").forEach(repeater => {
            const field = getFieldByPath(definition, repeater.dataset.sfRepeater!);
            if (!field) return;
            const config = (field.configuration ?? {}) as Partial<RepeaterFieldConfiguration>;
            const entries = getEntries(repeater);

            entries.forEach((entry, index) => {
                const title = entry.querySelector(":scope > [data-sf-entry-title]");
                if (title) title.textContent = getEntryTitle(field, index) ?? "";
                const removeButton = entry.querySelector<HTMLElement>(":scope > [data-sf-repeater-remove]");
                if (removeButton) removeButton.hidden = config.minItems != null && entries.length <= config.minItems;
            });

            const addButton = repeater.querySelector<HTMLElement>(":scope > [data-sf-repeater-add]");
            if (addButton) addButton.hidden = config.maxItems != null && entries.length >= config.maxItems;
        });
    }

    function renderPages(state: FormState) {
        const pages = Array.from(form.querySelectorAll<HTMLElement>("[data-sf-page]"));
        const visible = getVisiblePageIndexes(definition, state.values, state.pageIndex);
        const position = visible.indexOf(state.pageIndex);
        const page = definition.pages.find(it => it.index === state.pageIndex);
        const busy = state.status !== "idle";

        pages.forEach(element => {
            const index = Number(element.dataset.sfPage);
            element.hidden = index !== state.pageIndex;
            element.toggleAttribute("data-sf-skipped", !visible.includes(index));
        });

        const previousButton = form.querySelector<HTMLButtonElement>("[data-sf-previous]");
        if (previousButton) {
            previousButton.hidden = position <= 0;
            previousButton.textContent = page?.previousLabel ?? "";
            previousButton.disabled = busy;
        }
        const nextButton = form.querySelector<HTMLButtonElement>("[data-sf-next]");
        if (nextButton) {
            nextButton.hidden = position === visible.length - 1;
            nextButton.textContent = page?.nextLabel ?? "";
            nextButton.disabled = busy;
        }
        const submitButton = form.querySelector<HTMLButtonElement>("button[type=submit]");
        if (submitButton) {
            submitButton.hidden = position !== visible.length - 1;
            submitButton.disabled = busy;
        }

        const progress = form.querySelector<HTMLElement>("[data-sf-progress]");
        if (progress) {
            progress.hidden = false;
            progress.querySelectorAll<HTMLElement>("[data-sf-progress-step]").forEach(step => {
                const stepPosition = visible.indexOf(Number(step.dataset.sfProgressStep));
                step.hidden = stepPosition === -1;
                step.toggleAttribute("data-sf-complete", stepPosition !== -1 && stepPosition < position);
                if (stepPosition === position) step.setAttribute("aria-current", "step");
                else step.removeAttribute("aria-current");
            });
        }

        if (currentPage !== undefined && currentPage !== state.pageIndex) {
            form.dispatchEvent(new CustomEvent<PageChangeDetail>("sproutforms:pagechange", {
                bubbles: true,
                detail: { index: state.pageIndex, previousIndex: currentPage }
            }));
            // A page with errors leaves the focus to its first error
            if (Object.keys(state.errors).length === 0) pages.find(element => !element.hidden)?.focus();
        }
        currentPage = state.pageIndex;
    }

    function addEntry(repeater: HTMLElement) {
        const template = repeater.querySelector<HTMLTemplateElement>(":scope > [data-sf-repeater-template]");
        const container = repeater.querySelector(":scope > [data-sf-repeater-entries]");
        if (!template || !container) return;

        const fragment = template.content.cloneNode(true) as DocumentFragment;
        const entry = fragment.querySelector<HTMLElement>("[data-sf-repeater-entry]");
        if (!entry) return;

        container.appendChild(fragment);
        const path = repeater.dataset.sfRepeater!;
        renumberEntries(repeater, path);
        engine.addEntry(path);
        focusFirstInput(entry);
    }

    function removeEntry(entry: HTMLElement) {
        const repeater = entry.closest<HTMLElement>("[data-sf-repeater]");
        if (!repeater) return;

        const path = repeater.dataset.sfRepeater!;
        const index = getEntries(repeater).indexOf(entry);
        entry.remove();
        renumberEntries(repeater, path);
        engine.removeEntry(path, index);

        // Focus stays in the repeater: on the entry that took this one's place, the one before it, or the add button
        const remaining = getEntries(repeater);
        const next = remaining[index] ?? remaining[index - 1];
        if (next) focusFirstInput(next);
        else repeater.querySelector<HTMLElement>(":scope > [data-sf-repeater-add]")?.focus();
    }

    function focusFirstError() {
        const path = Object.keys(engine.getState().errors)[0];
        const wrapper = path ? form.querySelector<HTMLElement>(`[data-sf-field-id="${CSS.escape(path)}"]`) : null;
        if (wrapper) focusFirstInput(wrapper);
        else form.querySelector<HTMLElement>("[data-sf-global-errors]")?.scrollIntoView({ block: "nearest" });
    }

    function showSuccess(html: string) {
        const success = document.createElement("div");
        success.className = "form-success";
        success.setAttribute("data-sf-success", "");
        success.setAttribute("role", "status");
        success.innerHTML = html;
        form.replaceChildren(success);
    }

    return { render, addEntry, removeEntry, focusFirstError, showSuccess };
}

// Repeaters: the inputs of an entry are named after its index, such as "people[0].firstName", and the server reports its errors
// by the same index, so the entries are renumbered whenever one is added or removed
const renumberedAttributes = ["name", "id", "for", "data-sf-field-id", "data-field-id", "aria-describedby", "data-sf-entry-prefix", "data-sf-repeater"];
const indexPlaceholder = "__index__";

function getEntries(repeater: HTMLElement): HTMLElement[] {
    const container = repeater.querySelector(":scope > [data-sf-repeater-entries]");
    return container ? Array.from(container.querySelectorAll<HTMLElement>(":scope > [data-sf-repeater-entry]")) : [];
}

function renumberEntries(repeater: HTMLElement, path: string) {
    getEntries(repeater).forEach((entry, index) => renumberEntry(entry, path, index));
}

function renumberEntry(entry: HTMLElement, path: string, index: number) {
    const escapedPath = path.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
    const pattern = new RegExp(`(^|\\s)${escapedPath}\\[(?:\\d+|${indexPlaceholder})\\]`, "g");
    const replacement = `$1${path}[${index}]`;

    const rename = (element: Element) => {
        for (const attribute of renumberedAttributes) {
            const value = element.getAttribute(attribute);
            if (value) element.setAttribute(attribute, value.replace(pattern, replacement));
        }
    };

    // Including the templates of the repeaters inside the entry, so the entries added to them get this entry's index too
    const renameAll = (root: Element | DocumentFragment) => {
        root.querySelectorAll("*").forEach(element => {
            rename(element);
            if (element instanceof HTMLTemplateElement) renameAll(element.content);
        });
    };

    rename(entry);
    renameAll(entry);
}

// Helpers

// The elements of a field itself, not those of the fields inside it, such as the fields in a repeater's entries
function getOwnElements<T extends Element = Element>(field: Element, selector: string): T[] {
    return Array.from(field.querySelectorAll<T>(selector)).filter(element => element.closest("[data-sf-field-id]") === field);
}

function getFieldOfInput(target: EventTarget | null): HTMLElement | null {
    return isValueElement(target) ? target.closest<HTMLElement>("[data-sf-field-id]") : null;
}

function focusFirstInput(root: HTMLElement) {
    root.querySelector<HTMLElement>("input:not([type=hidden]), select, textarea")?.focus();
}

// The inline style keeps it hidden when a theme's classes set a display that would win over the hidden attribute
function setHidden(element: HTMLElement, hidden: boolean) {
    element.hidden = hidden;
    element.style.display = hidden ? "none" : "";
}

// forms.js finds errors by the data attribute, the class is only there for the default theme
function createErrorElement(): HTMLDivElement {
    const element = document.createElement("div");
    element.className = "form-error";
    element.setAttribute("data-sf-error", "");
    return element;
}

function escapeHtml(text: string): string {
    const element = document.createElement("div");
    element.textContent = text;
    return element.innerHTML;
}

export {};
