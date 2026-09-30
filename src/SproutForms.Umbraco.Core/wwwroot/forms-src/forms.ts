import { evaluateCondition } from "../../../SproutForms.Client/src/conditions";
import { builtInValidators } from "../../../SproutForms.Client/src/validation";
import type { ConditionDefinition } from "../../../SproutForms.Client/src/types";

interface SubmissionGuard {
    load?: (form: HTMLFormElement, settings: Record<string, unknown>) => Promise<void>;
    beforeSubmit?: (form: HTMLFormElement, settings: Record<string, unknown>, payload: FormData) => Promise<void>;
}

interface SubmissionGuardRegistry {
    [alias: string]: SubmissionGuard;
}

interface FieldCondition {
    rules: Array<{
        fieldAlias: string;
        comparison: string;
        value: unknown;
    }>;
    operator?: "All" | "Any";
}

interface FieldConditions {
    visibility?: FieldCondition;
    required?: FieldCondition;
}

interface Validator {
    (value: string | undefined, options: Record<string, string | undefined>, context?: Element): Promise<boolean>;
}

interface ValidatorRegistry {
    [alias: string]: Validator;
}

interface OutcomeHandler {
    (form: HTMLFormElement, outcomeData: Record<string, unknown>): void | Promise<void>;
}

interface OutcomeHandlerRegistry {
    [alias: string]: OutcomeHandler;
}

interface ValidationResult {
    valid: boolean;
    rule?: string;
    message?: string;
}

interface GuardSettings {
    siteKey?: string;
    action?: string;
}

interface GuardDefinition {
    alias: string;
    settings: Record<string, unknown>;
}

interface PageState {
    pages: HTMLElement[];
    conditions: Array<FieldCondition | undefined>;
    current: number;
}

interface PageChangeDetail {
    index: number;
    previousIndex: number;
}

interface FormSubmitResult {
    outcomeType?: string;
    outcomeData?: Record<string, unknown>;
    errors?: Record<string, string[]>;
    values?: Record<string, unknown>;
}

declare global {
    interface Window {
        SproutForms: {
            submissionGuard: {
                registry: SubmissionGuardRegistry;
                register(alias: string, guard: SubmissionGuard): void;
            };
            conditions: {
                registry: FieldCondition[] | null;
                init(form: Element): void;
                evaluate(fieldConditions: FieldCondition | undefined, formValues: Record<string, unknown>): boolean;
            };
            validation: {
                registry: ValidatorRegistry;
                register(alias: string, validator: Validator): void;
                validateField(fieldContainer: Element): Promise<ValidationResult>;
            };
            outcomeHandlers: {
                registry: OutcomeHandlerRegistry;
                register(alias: string, handler: OutcomeHandler): void;
            };
        };
        grecaptcha?: {
            execute(siteKey: string, options: { action: string }): Promise<string>;
        };
    }
}

window.SproutForms = {
    submissionGuard: {
        registry: {},
        register(alias: string, guard: SubmissionGuard) {
            this.registry[alias] = guard;
        }
    },
    conditions: {
        registry: null as FieldCondition[] | null,
        init(form: Element) {
            const raw = form.getAttribute("data-field-conditions");
            if (!raw) {
                this.registry = [];
                return;
            }
            try {
                this.registry = JSON.parse(raw);
            } catch {
                this.registry = [];
            }
        },
        // Shared with @sproutforms/client, so Razor and headless forms decide the same way as the server
        evaluate(fieldConditions: FieldCondition | undefined, formValues: Record<string, unknown>): boolean {
            return evaluateCondition(fieldConditions as ConditionDefinition | undefined, formValues);
        }
    },
    validation: {
        registry: {} as ValidatorRegistry,

        register(alias: string, validator: Validator) {
            this.registry[alias] = validator;
        },

        async validateField(fieldContainer: Element): Promise<ValidationResult> {
            const rules = (fieldContainer.getAttribute("data-sf-validate") || "").split(",");
            // A condition can make a field required that isn't required on its own
            if (getOwnElements(fieldContainer, "[data-conditional-required]").length > 0 && !rules.includes("required")) {
                rules.unshift("required");
            }
            const value = getFieldValue(fieldContainer);
            const containerEl = fieldContainer as HTMLElement;

            for (const rule of rules) {
                const validator = this.registry[rule.trim()];
                if (!validator) continue;

                const isValid = await validator(value, containerEl.dataset as Record<string, string | undefined>);

                if (!isValid) {
                    const capitalizedType = rule.charAt(0).toUpperCase() + rule.slice(1);
                    return {
                        valid: false,
                        rule,
                        message: containerEl.dataset[`sf${capitalizedType}Message`]
                            ?? (rule === "required" ? "Field is required." : undefined)
                    };
                }
            }

            return { valid: true };
        }
    },
    outcomeHandlers: {
        registry: {} as OutcomeHandlerRegistry,

        register(alias: string, handler: OutcomeHandler) {
            this.registry[alias] = handler;
        }
    }
};

document.addEventListener("submit", async function (e) {
    const form = e.target as HTMLFormElement;

    if (!form.matches("[data-form-ajax]")) return;

    e.preventDefault();

    // Enter in a field submits the form, which on any page but the last means going to the next page
    const pageState = pageStates.get(form);
    if (pageState && !isOnLastPage(pageState)) {
        await goToNextPage(form);
        return;
    }

    const validation = await validateAllFields(form);
    if (!validation) {
        showFirstPageWithError(form);
        return;
    }

    const formData = new FormData(form);

    const submissionGuards = getSubmissionGuards(form);
    try {
        for (const guardDef of submissionGuards) {
            const guard = window.SproutForms?.submissionGuard?.registry?.[guardDef.alias];
            if (!guard) continue;

            if (guard.beforeSubmit) {
                await guard.beforeSubmit(form, guardDef.settings, formData);
            }
        }
    } catch (err) {
        const error = err as Error;
        applyGlobalError(form, error.message || "Something went wrong. Please try again.");
        return;
    }

    const response = await fetch(form.action, {
        method: "POST",
        headers: {
            "X-Requested-With": "XMLHttpRequest"
        },
        body: formData
    });

    const result = await response.json() as FormSubmitResult;

    clearErrors(form);

    if (!response.ok) {
        applyErrors(form, result.errors || {});
        showFirstPageWithError(form);
        return;
    }

    if (result.outcomeType) {
        const handler = window.SproutForms?.outcomeHandlers?.registry?.[result.outcomeType];
        if (handler) {
            await handler(form, result.outcomeData);
            return;
        }
    }

    // The submission was saved, but there's no outcome to show (none configured, or no handler registered for it).
    // Confirm it anyway, so the visitor doesn't think it failed and submit again.
    showFallbackSuccess(form);
});

// The value the field submits: the checked radio, a checkbox only when it's checked, and the file name of an upload.
// A repeater's value is the number of entries the visitor filled in, as the server counts them
function getFieldValue(fieldContainer: Element): string | undefined {
    const repeater = getOwnElements<HTMLElement>(fieldContainer, "[data-sf-repeater]")[0];
    if (repeater) {
        const count = getEntries(repeater).filter(entry => !isBlankEntry(entry)).length;
        return count === 0 ? undefined : String(count);
    }

    const inputs = getOwnElements<HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement>(fieldContainer, "input, textarea, select");
    const first = inputs[0];
    if (!first) return undefined;

    if (first instanceof HTMLInputElement && (first.type === "radio" || first.type === "checkbox")) {
        const checked = inputs.find(input => input instanceof HTMLInputElement && input.type === first.type && input.checked);
        return checked?.value;
    }
    if (first instanceof HTMLInputElement && first.type === "file") {
        return first.files?.[0]?.name;
    }
    return first.value;
}

function showFallbackSuccess(form: HTMLFormElement) {
    const success = document.createElement("div");
    success.className = "form-success";
    success.setAttribute("data-sf-success", "");
    success.setAttribute("role", "status");
    success.textContent = "Thank you, your submission has been received.";
    form.replaceChildren(success);
}

document.addEventListener("DOMContentLoaded", () => {
    document.querySelectorAll("form[data-form-ajax]").forEach(initForm);
});

function initForm(form: Element) {
    initFormGuards(form as HTMLFormElement);
    initRepeaters(form as HTMLFormElement);
    initConditionalFields(form as HTMLFormElement);
    initPageUrl(form as HTMLFormElement);
    initValidation(form as HTMLFormElement);
    initPages(form as HTMLFormElement);
}

// The elements of a field itself, not those of the fields inside it, such as the fields in a repeater's entries
function getOwnElements<T extends Element = Element>(field: Element, selector: string): T[] {
    return Array.from(field.querySelectorAll<T>(selector)).filter(element => element.closest("[data-sf-field-id]") === field);
}

// Repeaters: the visitor adds and removes entries, whose inputs are named after their index, such as "people[0].firstName".
// The server reports an entry's errors by the same index, so the entries are renumbered whenever one is removed
const renumberedAttributes = ["name", "id", "for", "data-sf-field-id", "data-field-id", "aria-describedby", "data-sf-entry-prefix", "data-sf-repeater"];
const indexPlaceholder = "__index__";

function initRepeaters(form: HTMLFormElement) {
    form.querySelectorAll<HTMLElement>("[data-sf-repeater]").forEach(updateRepeater);

    form.addEventListener("click", event => {
        const target = event.target as Element;
        const addButton = target.closest("[data-sf-repeater-add]");
        if (addButton) {
            addEntry(form, addButton.closest<HTMLElement>("[data-sf-repeater]")!);
            return;
        }
        const removeButton = target.closest("[data-sf-repeater-remove]");
        if (removeButton) {
            removeEntry(form, removeButton.closest<HTMLElement>("[data-sf-repeater-entry]")!);
        }
    });
}

function getEntries(repeater: HTMLElement): HTMLElement[] {
    const container = repeater.querySelector(":scope > [data-sf-repeater-entries]");
    return container ? Array.from(container.querySelectorAll<HTMLElement>(":scope > [data-sf-repeater-entry]")) : [];
}

// An entry whose fields are all empty isn't sent on by the server, so it isn't validated here either
function isBlankEntry(entry: HTMLElement): boolean {
    return Array.from(entry.querySelectorAll("[data-sf-field-id]"))
        .filter(field => field.closest("[data-sf-repeater-entry]") === entry)
        .every(field => !getFieldValue(field)?.trim());
}

function isInBlankEntry(element: Element): boolean {
    for (let entry = element.closest<HTMLElement>("[data-sf-repeater-entry]"); entry; entry = entry.parentElement?.closest<HTMLElement>("[data-sf-repeater-entry]") ?? null) {
        if (isBlankEntry(entry)) return true;
    }
    return false;
}

function addEntry(form: HTMLFormElement, repeater: HTMLElement) {
    const template = repeater.querySelector<HTMLTemplateElement>(":scope > [data-sf-repeater-template]");
    const container = repeater.querySelector(":scope > [data-sf-repeater-entries]");
    if (!template || !container) return;

    const fragment = template.content.cloneNode(true) as DocumentFragment;
    const entry = fragment.querySelector<HTMLElement>("[data-sf-repeater-entry]");
    if (!entry) return;

    container.appendChild(fragment);
    updateRepeater(repeater);
    initFieldConditions(entry);
    evaluateAllConditions(form);

    const field = repeater.closest<HTMLElement>("[data-sf-field-id]");
    if (field) clearFieldError(field);
    focusFirstInput(entry);
}

function removeEntry(form: HTMLFormElement, entry: HTMLElement) {
    const repeater = entry.closest<HTMLElement>("[data-sf-repeater]");
    if (!repeater) return;

    const index = getEntries(repeater).indexOf(entry);
    entry.remove();
    updateRepeater(repeater);
    evaluateAllConditions(form);

    // Focus stays in the repeater: on the entry that took this one's place, the one before it, or the add button
    const remaining = getEntries(repeater);
    const next = remaining[index] ?? remaining[index - 1];
    if (next) {
        focusFirstInput(next);
    } else {
        repeater.querySelector<HTMLElement>(":scope > [data-sf-repeater-add]")?.focus();
    }
}

function focusFirstInput(root: HTMLElement) {
    root.querySelector<HTMLElement>("input:not([type=hidden]), select, textarea")?.focus();
}

// Numbers the entries in order, titles them, and shows the add and remove buttons the minimum and maximum allow
function updateRepeater(repeater: HTMLElement) {
    const alias = repeater.dataset.sfRepeater ?? "";
    const titleTemplate = repeater.dataset.sfItemTitle ?? "";
    const min = parseInt(repeater.dataset.sfRepeaterMin ?? "", 10);
    const max = parseInt(repeater.dataset.sfRepeaterMax ?? "", 10);
    const entries = getEntries(repeater);

    entries.forEach((entry, index) => {
        renumberEntry(entry, alias, index);
        const title = entry.querySelector(":scope > [data-sf-entry-title]");
        if (title) title.textContent = titleTemplate.replace(/\{n\}/g, String(index + 1));
        const removeButton = entry.querySelector<HTMLElement>(":scope > [data-sf-repeater-remove]");
        if (removeButton) removeButton.hidden = !isNaN(min) && entries.length <= min;
    });

    const addButton = repeater.querySelector<HTMLElement>(":scope > [data-sf-repeater-add]");
    if (addButton) addButton.hidden = !isNaN(max) && entries.length >= max;
}

function renumberEntry(entry: HTMLElement, alias: string, index: number) {
    const escapedAlias = alias.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
    const pattern = new RegExp(`(^|\\s)${escapedAlias}\\[(?:\\d+|${indexPlaceholder})\\]`, "g");
    const replacement = `$1${alias}[${index}]`;

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

const pageStates = new WeakMap<HTMLFormElement, PageState>();

function initPages(form: HTMLFormElement) {
    if (!form.hasAttribute("data-sf-paged")) return;

    const pages = Array.from(form.querySelectorAll<HTMLElement>("[data-sf-page]"));
    const state: PageState = {
        pages,
        conditions: pages.map(page => parsePageConditions(page)),
        current: 0
    };
    pageStates.set(form, state);
    updatePageVisibility(form);

    form.querySelector("[data-sf-previous]")?.addEventListener("click", () => goToPreviousPage(form));
    form.querySelector("[data-sf-next]")?.addEventListener("click", () => goToNextPage(form));
    form.querySelector("[data-sf-progress]")?.removeAttribute("hidden");

    // After a post without JavaScript the errors are already rendered, so start on the first page that has one
    const pageWithError = findFirstPageWithError(state);
    showPage(form, pageWithError === -1 ? 0 : pageWithError, false);
}

function parsePageConditions(page: HTMLElement): FieldCondition | undefined {
    const raw = page.getAttribute("data-sf-page-conditions");
    if (!raw) return undefined;

    try {
        return JSON.parse(raw);
    } catch (e) {
        console.error("Failed to parse page conditions:", e);
        return undefined;
    }
}

// Marks the pages whose conditions don't hold as skipped, and updates the buttons and progress to match
function updatePageVisibility(form: HTMLFormElement) {
    const state = pageStates.get(form);
    if (!state) return;

    const formValues = getFormValues(form);
    state.pages.forEach((page, index) => {
        const isVisible = window.SproutForms.conditions.evaluate(state.conditions[index], formValues);
        page.toggleAttribute("data-sf-skipped", !isVisible);
    });

    updatePageNavigation(form, state);
    updateProgress(form, state);
}

// The current page always counts as visible: its conditions only depend on earlier pages
function getVisiblePageIndexes(state: PageState): number[] {
    return state.pages
        .map((_, index) => index)
        .filter(index => index === state.current || !state.pages[index].hasAttribute("data-sf-skipped"));
}

function isOnLastPage(state: PageState): boolean {
    const visible = getVisiblePageIndexes(state);
    return visible.indexOf(state.current) === visible.length - 1;
}

function showPage(form: HTMLFormElement, index: number, moveFocus: boolean) {
    const state = pageStates.get(form);
    if (!state) return;

    const previousIndex = state.current;
    state.current = index;
    state.pages.forEach((page, i) => page.hidden = i !== index);

    updatePageNavigation(form, state);
    updateProgress(form, state);

    if (moveFocus) {
        state.pages[index].focus();
    }
    if (previousIndex !== index) {
        form.dispatchEvent(new CustomEvent<PageChangeDetail>("sproutforms:pagechange", {
            bubbles: true,
            detail: { index, previousIndex }
        }));
    }
}

function updatePageNavigation(form: HTMLFormElement, state: PageState) {
    const visible = getVisiblePageIndexes(state);
    const position = visible.indexOf(state.current);
    const page = state.pages[state.current];
    const isLast = position === visible.length - 1;

    const previousButton = form.querySelector<HTMLButtonElement>("[data-sf-previous]");
    if (previousButton) {
        previousButton.hidden = position <= 0;
        previousButton.textContent = page.dataset.sfPreviousLabel ?? "";
    }

    const nextButton = form.querySelector<HTMLButtonElement>("[data-sf-next]");
    if (nextButton) {
        nextButton.hidden = isLast;
        nextButton.textContent = page.dataset.sfNextLabel ?? "";
    }

    const submitButton = form.querySelector<HTMLButtonElement>("button[type=submit]");
    if (submitButton) {
        submitButton.hidden = !isLast;
    }
}

function updateProgress(form: HTMLFormElement, state: PageState) {
    const visible = getVisiblePageIndexes(state);
    const currentPosition = visible.indexOf(state.current);

    form.querySelectorAll<HTMLElement>("[data-sf-progress-step]").forEach(step => {
        const index = parseInt(step.dataset.sfProgressStep ?? "", 10);
        const position = visible.indexOf(index);
        const isCurrent = index === state.current;

        step.hidden = position === -1;
        step.toggleAttribute("data-sf-complete", position !== -1 && position < currentPosition);
        if (isCurrent) {
            step.setAttribute("aria-current", "step");
        } else {
            step.removeAttribute("aria-current");
        }
    });
}

async function goToNextPage(form: HTMLFormElement) {
    const state = pageStates.get(form);
    if (!state) return;

    const page = state.pages[state.current];
    const isValid = await validateAllFields(page);
    if (!isValid) return;

    const nextButton = form.querySelector<HTMLButtonElement>("[data-sf-next]");
    if (nextButton) nextButton.disabled = true;
    try {
        if (!await validatePageOnServer(form, state.current)) return;
    } finally {
        if (nextButton) nextButton.disabled = false;
    }

    const visible = getVisiblePageIndexes(state);
    const next = visible[visible.indexOf(state.current) + 1];
    if (next !== undefined) {
        showPage(form, next, true);
    }
}

// Checks the rules only the server knows. Uploads aren't sent; they're checked when the form is submitted.
async function validatePageOnServer(form: HTMLFormElement, pageIndex: number): Promise<boolean> {
    const formData = new FormData(form);
    form.querySelectorAll<HTMLInputElement>("input[type=file]").forEach(input => formData.delete(input.name));

    let response: Response;
    try {
        response = await fetch(`${form.action}/pages/${pageIndex}/validate`, {
            method: "POST",
            headers: {
                "X-Requested-With": "XMLHttpRequest"
            },
            body: formData
        });
    } catch {
        applyGlobalError(form, "Something went wrong. Please try again.");
        return false;
    }

    const page = pageStates.get(form)?.pages[pageIndex];
    if (page) clearErrors(page);

    if (response.status === 400) {
        const result = await response.json() as FormSubmitResult;
        applyErrors(form, result.errors || {});
        return false;
    }
    if (!response.ok) {
        applyGlobalError(form, "Something went wrong. Please try again.");
        return false;
    }
    return true;
}

function goToPreviousPage(form: HTMLFormElement) {
    const state = pageStates.get(form);
    if (!state) return;

    const visible = getVisiblePageIndexes(state);
    const previous = visible[visible.indexOf(state.current) - 1];
    if (previous !== undefined) {
        showPage(form, previous, true);
    }
}

function findFirstPageWithError(state: PageState): number {
    return state.pages.findIndex(page => page.querySelector("[data-sf-error]"));
}

function showFirstPageWithError(form: HTMLFormElement) {
    const state = pageStates.get(form);
    if (!state) return;

    const index = findFirstPageWithError(state);
    if (index !== -1 && index !== state.current) {
        showPage(form, index, true);
    }
}

function initPageUrl(form: HTMLFormElement) {
    const pageUrlInput = form.querySelector('[data-sf-page-url="true"]') as HTMLInputElement | null;
    if (pageUrlInput) {
        pageUrlInput.value = window.location.href;
    }
}

// Listens on the form, so the fields of a repeater entry added later are validated too
function initValidation(form: HTMLFormElement) {
    form.addEventListener("focusout", async event => {
        const group = getFieldOfInput(event.target);
        if (group?.hasAttribute("data-sf-validate")) {
            await validateGroup(group);
        }
    });

    form.addEventListener("input", event => {
        const group = getFieldOfInput(event.target);
        if (group?.hasAttribute("data-sf-validate")) {
            clearFieldError(group);
        }
    });
}

function getFieldOfInput(target: EventTarget | null): HTMLElement | null {
    if (!(target instanceof HTMLInputElement || target instanceof HTMLSelectElement || target instanceof HTMLTextAreaElement)) return null;
    return target.closest<HTMLElement>("[data-sf-field-id]");
}

async function validateGroup(group: HTMLElement): Promise<boolean> {
    const result = await window.SproutForms.validation.validateField(group);

    if (!result.valid) {
        showFieldError(group, result.message);
        return false;
    }

    clearFieldError(group);
    return true;
}

function showFieldError(group: HTMLElement, message?: string) {
    getOwnElements(group, "input, select, textarea").forEach(input => input.setAttribute("aria-invalid", "true"));
    getOwnElements(group, "[data-sf-error]").forEach(error => error.remove());

    const errorContainer = createErrorElement();
    errorContainer.setAttribute("role", "alert");
    errorContainer.textContent = message ?? "";

    group.appendChild(errorContainer);
}

function clearFieldError(group: HTMLElement) {
    getOwnElements(group, "input, select, textarea").forEach(input => input.setAttribute("aria-invalid", "false"));
    getOwnElements(group, "[data-sf-error]").forEach(error => error.remove());
}

// Validates the fields inside root: the whole form, or a single page
async function validateAllFields(root: ParentNode): Promise<boolean> {
    let isValid = true;
    // Fields with conditions too, since a condition can make them required
    const groups = root.querySelectorAll("[data-sf-validate], [data-condition-field]");

    for (const group of groups) {
        const groupEl = group as HTMLElement;
        // Also the fields inside a hidden repeater
        if (groupEl.closest("[data-sf-field-id][hidden], [data-sf-col][hidden]")) {
            continue;
        }
        // A skipped page's fields aren't validated, on the server either
        if (groupEl.closest("[data-sf-skipped]")) {
            continue;
        }
        // Neither are the fields of a repeater entry the visitor left empty
        if (isInBlankEntry(groupEl)) {
            clearFieldError(groupEl);
            continue;
        }

        const result = await validateGroup(groupEl);
        if (!result) {
            isValid = false;
        }
    }

    return isValid;
}

// Listens on the form, so the fields of a repeater entry added later are evaluated too
function initConditionalFields(form: HTMLFormElement) {
    initFieldConditions(form);

    form.addEventListener("input", () => evaluateAllConditions(form));
    form.addEventListener("change", () => evaluateAllConditions(form));

    evaluateAllConditions(form);
}

function initFieldConditions(root: ParentNode) {
    const formFields = root.querySelectorAll("[data-field-conditions]");

    formFields.forEach(wrapper => {
        const fieldAlias = wrapper.getAttribute("data-sf-field-id");
        const conditionsRaw = wrapper.getAttribute("data-field-conditions");

        if (!conditionsRaw || !fieldAlias) return;

        try {
            const conditions: FieldConditions = JSON.parse(conditionsRaw);
            wrapper.setAttribute("data-condition-field", fieldAlias);
            (wrapper as HTMLElement & { conditions: FieldConditions }).conditions = conditions;
        } catch (e) {
            console.error("Failed to parse field conditions:", e);
        }
    });
}

function getFormValues(form: HTMLFormElement): Record<string, unknown> {
    const values: Record<string, unknown> = {};
    const formData = new FormData(form);
    for (const [key, value] of formData.entries()) {
        values[key] = value;
    }

    // The value the server receives: the checkbox's value when checked, otherwise the hidden "false" next to it
    const checkboxes = form.querySelectorAll("input[type='checkbox']");
    checkboxes.forEach(cb => {
        const checkbox = cb as HTMLInputElement;
        values[checkbox.name] = checkbox.checked ? checkbox.value : "false";
    });

    return values;
}

function getScopedValues(element: Element, formValues: Record<string, unknown>): Record<string, unknown> {
    const entries: HTMLElement[] = [];
    for (let entry = element.closest<HTMLElement>("[data-sf-repeater-entry]"); entry; entry = entry.parentElement?.closest<HTMLElement>("[data-sf-repeater-entry]") ?? null) {
        entries.unshift(entry);
    }
    if (entries.length === 0) return formValues;

    const values = { ...formValues };
    for (const entry of entries) {
        const prefix = entry.dataset.sfEntryPrefix ?? "";
        for (const [name, value] of Object.entries(formValues)) {
            if (name.startsWith(prefix)) values[name.slice(prefix.length)] = value;
        }
    }
    return values;
}

function evaluateAllConditions(form: HTMLFormElement) {
    const formValues = getFormValues(form);
    const fields = form.querySelectorAll("[data-condition-field]");

    fields.forEach(wrapper => {
        const wrapperEl = wrapper as HTMLElement & { conditions?: FieldConditions };
        const conditions = wrapperEl.conditions;
        if (!conditions) return;

        const visibilityCondition = conditions.visibility;
        const requiredCondition = conditions.required;

        // Inside a repeater entry, a condition sees the entry's own fields by their alias, over the form's
        const values = getScopedValues(wrapperEl, formValues);
        const isVisible = window.SproutForms.conditions.evaluate(visibilityCondition, values);

        setHidden(wrapperEl, !isVisible);
        const parentCol = wrapper.closest<HTMLElement>("[data-sf-col]");
        if (parentCol) {
            setHidden(parentCol, !isVisible);
        }

        getOwnElements(wrapper, "[data-conditional-required]").forEach(existingRequired => {
            existingRequired.removeAttribute("data-conditional-required");
            existingRequired.removeAttribute("required");
        });

        if (isVisible && requiredCondition && window.SproutForms.conditions.evaluate(requiredCondition, values)) {
            // A repeater marks itself, since its inputs belong to its entries' fields
            const input = getOwnElements(wrapper, "input, select, textarea, [data-sf-repeater]")[0];
            if (input) {
                input.setAttribute("data-conditional-required", "true");
                input.setAttribute("required", "");
            }
        }
    });

    updatePageVisibility(form);
}

// The inline style keeps it hidden when a theme's classes set a display that would win over the hidden attribute
function setHidden(element: HTMLElement, hidden: boolean) {
    element.hidden = hidden;
    element.style.display = hidden ? "none" : "";
}

window.SproutForms.submissionGuard.register("recaptchaV3", {
    async load(form: HTMLFormElement, settings: GuardSettings) {
        if (window.grecaptcha) return;

        await loadScript(
            `https://www.google.com/recaptcha/api.js?render=${settings.siteKey}`
        );
    },

    async beforeSubmit(form: HTMLFormElement, settings: GuardSettings, payload: FormData) {
        const token = await window.grecaptcha!.execute(settings.siteKey!, {
            action: settings.action || "submit"
        });

        payload.append("g-recaptcha-response", token);
    }
});

// The built-in rules are shared with @sproutforms/client; forms.js reads a rule's value from the field's data attribute.
// None of them look at the field or the other values, so they get an empty context
const emptyValidatorContext = { field: { alias: "", label: "", type: "", required: false, rendersOwnLabel: false, validationRules: [] }, values: {} };
for (const [type, validator] of Object.entries(builtInValidators)) {
    window.SproutForms.validation.register(type, async (value, options) => {
        if (!value) return type !== "required";
        const capitalizedType = type.charAt(0).toUpperCase() + type.slice(1);
        return validator(value, { type, value: options[`sf${capitalizedType}`] }, emptyValidatorContext);
    });
}

// A repeater's value is the number of entries the visitor filled in; without any, it has no value
window.SproutForms.validation.register("minItems", async (value, options) => Number(value ?? 0) >= Number(options.sfMinItems));
window.SproutForms.validation.register("maxItems", async (value, options) => Number(value ?? 0) <= Number(options.sfMaxItems));

window.SproutForms.validation.register("sameAs", async (value, options, context) => {
    if (!context) return false;
    const other = context.querySelector(`[name="${options.sfOther}"]`) as HTMLInputElement | null;
    return other && value === other.value;
});

// Clears the errors inside root: the whole form, or a single page
function clearErrors(root: ParentNode) {
    root.querySelectorAll("[data-sf-error]").forEach(e => e.remove());
    root.querySelectorAll("[aria-invalid]").forEach(el => {
        el.setAttribute("aria-invalid", "false");
    });
}

// The inputs still hold what the visitor entered, so only the errors are applied. Writing the
// echoed values back would overwrite the value attribute of checkboxes and radios.
function applyErrors(form: HTMLFormElement, errors: Record<string, string[]>) {
    for (const fieldId in errors) {
        const messages = errors[fieldId];
        const wrapper = form.querySelector(`[data-sf-field-id="${fieldId}"]`)
            ?? form.querySelector(`[name="${fieldId}"]`)?.closest("[data-sf-field-id]");

        if (!wrapper) {
            messages.forEach(msg => {
                applyGlobalError(form, msg);
            });
            continue;
        }

        getOwnElements(wrapper, "input, select, textarea").forEach(input => input.setAttribute("aria-invalid", "true"));

        const errorContainer = createErrorElement();

        messages.forEach(msg => {
            const div = document.createElement("div");
            div.textContent = msg;
            errorContainer.appendChild(div);
        });

        wrapper.appendChild(errorContainer);
    }
}

function applyGlobalError(form: HTMLFormElement, message: string) {
    const container = getOrCreateGlobalErrorContainer(form);

    const div = createErrorElement();
    div.textContent = message;

    container.appendChild(div);
}

// forms.js finds errors by the data attribute, the class is only there for the default theme
function createErrorElement(): HTMLDivElement {
    const element = document.createElement("div");
    element.className = "form-error";
    element.setAttribute("data-sf-error", "");
    return element;
}

function getOrCreateGlobalErrorContainer(form: HTMLFormElement): HTMLElement {
    let container = form.querySelector<HTMLElement>("[data-sf-global-errors]");

    if (!container) {
        container = document.createElement("div");
        container.className = "form-global-errors";
        container.setAttribute("data-sf-global-errors", "");
        container.setAttribute("role", "alert");
        container.setAttribute("aria-live", "assertive");
        form.prepend(container);
    }

    return container;
}

async function initFormGuards(form: HTMLFormElement) {
    const submissionGuards = getSubmissionGuards(form);

    for (const guardDef of submissionGuards) {
        const guard = window.SproutForms?.submissionGuard?.registry?.[guardDef.alias];
        if (!guard || !guard.load) continue;

        try {
            await guard.load(form, guardDef.settings);
        } catch (err) {
            const error = err as Error;
            console.error(`Enhancer '${guardDef.alias}' failed to load`, error);
            applyGlobalError(
                form,
                "This form could not be initialized correctly. Please try again later."
            );
        }
    }
}

function loadScript(src: string): Promise<void> {
    return new Promise((resolve, reject) => {
        const s = document.createElement("script");
        s.src = src;
        s.async = true;
        s.onload = () => resolve();
        s.onerror = () => reject(new Error(`Failed to load script: ${src}`));
        document.head.appendChild(s);
    });
}

function getSubmissionGuards(form: HTMLFormElement): GuardDefinition[] {
    const raw = form.getAttribute("data-submission-guards");
    if (!raw) return [];

    try {
        return JSON.parse(raw);
    } catch {
        return [];
    }
}

window.SproutForms.outcomeHandlers.register("message", (form, outcomeData) => {
    const message = outcomeData.message as string;
    if (message) {
        form.innerHTML = `<div class="form-success" data-sf-success role="status">${message}</div>`;
    }
});

window.SproutForms.outcomeHandlers.register("redirect", (form, outcomeData) => {
    const url = outcomeData.url as string;
    if (url) {
        window.location.href = url;
    }
});

window.SproutForms.outcomeHandlers.register("redirectUmbracoPage", (form, outcomeData) => {
    const url = outcomeData.url as string;
    if (url) {
        window.location.href = url;
    }
});

export {}