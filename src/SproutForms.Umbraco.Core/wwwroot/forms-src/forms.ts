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
        evaluate(fieldConditions: FieldCondition | undefined, formValues: Record<string, unknown>): boolean {
            if (!fieldConditions || !fieldConditions.rules || fieldConditions.rules.length === 0) {
                return true;
            }
            const rules = fieldConditions.rules;
            const operator = fieldConditions.operator || "All";

            const results = rules.map(rule => {
                const fieldValue = formValues[rule.fieldAlias];
                const targetValue = rule.value;

                switch (rule.comparison) {
                    case "Equals":
                        return String(fieldValue || "").toLowerCase() === String(targetValue || "").toLowerCase();
                    case "NotEquals":
                        return String(fieldValue || "").toLowerCase() !== String(targetValue || "").toLowerCase();
                    case "Contains":
                        return String(fieldValue || "").toLowerCase().includes(String(targetValue || "").toLowerCase());
                    case "GreaterThan":
                        return parseFloat(String(fieldValue)) > parseFloat(String(targetValue));
                    case "LessThan":
                        return parseFloat(String(fieldValue)) < parseFloat(String(targetValue));
                    case "IsEmpty":
                        return !fieldValue || String(fieldValue).trim() === "";
                    case "IsNotEmpty":
                        return fieldValue && String(fieldValue).trim() !== "";
                    case "MatchesRegex":
                        try {
                            return new RegExp(String(targetValue)).test(String(fieldValue || ""));
                        } catch {
                            return false;
                        }
                    case "DoesNotMatchRegex":
                        try {
                            return !new RegExp(String(targetValue)).test(String(fieldValue || ""));
                        } catch {
                            return false;
                        }
                    default:
                        return true;
                }
            });

            if (operator === "All") {
                return results.every(r => r === true);
            } else {
                return results.some(r => r === true);
            }
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
            if (fieldContainer.querySelector("[data-conditional-required]") && !rules.includes("required")) {
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

// The value the field submits: the checked radio, a checkbox only when it's checked, and the file name of an upload
function getFieldValue(fieldContainer: Element): string | undefined {
    const inputs = Array.from(fieldContainer.querySelectorAll<HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement>("input, textarea, select"));
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
    success.setAttribute("role", "status");
    success.textContent = "Thank you, your submission has been received.";
    form.replaceChildren(success);
}

document.addEventListener("DOMContentLoaded", () => {
    document.querySelectorAll("form[data-form-ajax]").forEach(initForm);
});

function initForm(form: Element) {
    initFormGuards(form as HTMLFormElement);
    initConditionalFields(form as HTMLFormElement);
    initPageUrl(form as HTMLFormElement);
    initValidation(form as HTMLFormElement);
    initPages(form as HTMLFormElement);
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
    form.querySelector(".form-progress")?.removeAttribute("hidden");

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
        page.classList.toggle("sf-page-skipped", !isVisible);
    });

    updatePageNavigation(form, state);
    updateProgress(form, state);
}

// The current page always counts as visible: its conditions only depend on earlier pages
function getVisiblePageIndexes(state: PageState): number[] {
    return state.pages
        .map((_, index) => index)
        .filter(index => index === state.current || !state.pages[index].classList.contains("sf-page-skipped"));
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
        step.classList.toggle("is-current", isCurrent);
        step.classList.toggle("is-complete", position !== -1 && position < currentPosition);
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
    return state.pages.findIndex(page => page.querySelector(".form-error"));
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

function initValidation(form: HTMLFormElement) {
    const validateGroups = form.querySelectorAll("[data-sf-validate]");

    validateGroups.forEach(group => {
        const inputs = group.querySelectorAll("input, select, textarea");

        inputs.forEach(input => {
            input.addEventListener("blur", async () => await validateGroup(group as HTMLElement));
        });

        inputs.forEach(input => {
            input.addEventListener("input", () => clearFieldError(group as HTMLElement));
        });
    });
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
    const inputs = group.querySelectorAll("input, select, textarea");
    inputs.forEach(input => input.setAttribute("aria-invalid", "true"));

    const existingError = group.querySelector(".form-error");
    if (existingError) {
        existingError.remove();
    }

    const errorContainer = document.createElement("div");
    errorContainer.className = "form-error";
    errorContainer.setAttribute("role", "alert");
    errorContainer.textContent = message;

    group.appendChild(errorContainer);
}

function clearFieldError(group: HTMLElement) {
    const inputs = group.querySelectorAll("input, select, textarea");
    inputs.forEach(input => input.setAttribute("aria-invalid", "false"));

    const existingError = group.querySelector(".form-error");
    if (existingError) {
        existingError.remove();
    }
}

// Validates the fields inside root: the whole form, or a single page
async function validateAllFields(root: ParentNode): Promise<boolean> {
    let isValid = true;
    // Fields with conditions too, since a condition can make them required
    const groups = root.querySelectorAll("[data-sf-validate], [data-condition-field]");

    for (const group of groups) {
        const groupEl = group as HTMLElement;
        if (groupEl.classList.contains("sf-hidden") || groupEl.style.display === "none") {
            continue;
        }
        // A skipped page's fields aren't validated, on the server either
        if (groupEl.closest(".sf-page-skipped")) {
            continue;
        }

        const result = await validateGroup(groupEl);
        if (!result) {
            isValid = false;
        }
    }

    return isValid;
}

function initConditionalFields(form: HTMLFormElement) {
    const formFields = form.querySelectorAll("[data-field-conditions]");

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

    const allInputs = form.querySelectorAll("input, select, textarea");
    allInputs.forEach(input => {
        input.addEventListener("input", () => evaluateAllConditions(form));
        input.addEventListener("change", () => evaluateAllConditions(form));
    });

    evaluateAllConditions(form);
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

function evaluateAllConditions(form: HTMLFormElement) {
    const formValues = getFormValues(form);
    const fields = form.querySelectorAll("[data-condition-field]");

    fields.forEach(wrapper => {
        const wrapperEl = wrapper as HTMLElement & { conditions?: FieldConditions };
        const conditions = wrapperEl.conditions;
        if (!conditions) return;

        const visibilityCondition = conditions.visibility;
        const requiredCondition = conditions.required;

        const isVisible = window.SproutForms.conditions.evaluate(visibilityCondition, formValues);

        const parentCol = wrapper.closest(".form-col") as HTMLElement | null;

        if (isVisible) {
            wrapperEl.style.display = "";
            wrapperEl.removeAttribute("hidden");
            wrapperEl.classList.remove("sf-hidden");
            if (parentCol) {
                parentCol.style.display = "";
                parentCol.classList.remove("sf-hidden");
            }
        } else {
            wrapperEl.style.display = "none";
            wrapperEl.setAttribute("hidden", "");
            wrapperEl.classList.add("sf-hidden");
            if (parentCol) {
                parentCol.style.display = "none";
                parentCol.classList.add("sf-hidden");
            }
        }

        const existingRequired = wrapper.querySelector("[data-conditional-required]");
        if (existingRequired) {
            existingRequired.removeAttribute("data-conditional-required");
            existingRequired.removeAttribute("required");
        }

        if (isVisible && requiredCondition && window.SproutForms.conditions.evaluate(requiredCondition, formValues)) {
            const input = wrapper.querySelector("input, select, textarea") as HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement | null;
            if (input) {
                input.setAttribute("data-conditional-required", "true");
                input.setAttribute("required", "");
            }
        }
    });

    updatePageVisibility(form);
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

window.SproutForms.validation.register("required", async (value) => {
    if (!value) return false;
    return value.trim().length > 0;
});

window.SproutForms.validation.register("minLength", async (value, options) => {
    if (!value) return true;
    return value.length >= parseInt(options.sfMinLength || "0");
});

window.SproutForms.validation.register("maxLength", async (value, options) => {
    if (!value) return true;
    return value.length <= parseInt(options.sfMaxLength || "999999");
});

window.SproutForms.validation.register("sameAs", async (value, options, context) => {
    if (!context) return false;
    const other = context.querySelector(`[name="${options.sfOther}"]`) as HTMLInputElement | null;
    return other && value === other.value;
});

window.SproutForms.validation.register("regex", async (value, options) => {
    if (!value) return true;

    const pattern = options.sfRegex;
    if (!pattern) return true;

    let regex: RegExp;

    try {
        regex = new RegExp(pattern);
    } catch (e) {
        console.warn("Invalid regex pattern:", pattern);
        return false;
    }

    return regex.test(value);
});

window.SproutForms.validation.register("minDate", async (value, options) => {
    if (!value) return true;

    const minDateValue = options.sfMinDate;
    if (!minDateValue) return true;

    const inputDate = new Date(value);
    const minDate = new Date(minDateValue);

    if (isNaN(inputDate.getTime()) || isNaN(minDate.getTime())) {
        console.warn("Invalid date value in minDate validator.");
        return true;
    }

    if (inputDate >= minDate) {
        return true;
    }

    return false;
});

window.SproutForms.validation.register("maxDate", async (value, options) => {
    if (!value) return true;

    const maxDateValue = options.sfMaxDate;
    if (!maxDateValue) return true;

    const inputDate = new Date(value);
    const maxDate = new Date(maxDateValue);

    if (isNaN(inputDate.getTime()) || isNaN(maxDate.getTime())) {
        console.warn("Invalid date value in maxDate validator.");
        return true;
    }

    if (inputDate <= maxDate) {
        return true;
    }

    return false;
});

// Clears the errors inside root: the whole form, or a single page
function clearErrors(root: ParentNode) {
    root.querySelectorAll(".form-error").forEach(e => e.remove());
    root.querySelectorAll("[aria-invalid]").forEach(el => {
        el.setAttribute("aria-invalid", "false");
    });
}

// The inputs still hold what the visitor entered, so only the errors are applied. Writing the
// echoed values back would overwrite the value attribute of checkboxes and radios.
function applyErrors(form: HTMLFormElement, errors: Record<string, string[]>) {
    for (const fieldId in errors) {
        const messages = errors[fieldId];
        const input = form.querySelector(`[data-sf-field-id="${fieldId}"]`)
            || form.querySelector(`[name="${fieldId}"]`);

        if (!input) {
            messages.forEach(msg => {
                applyGlobalError(form, msg);
            });
            continue;
        }

        input.setAttribute("aria-invalid", "true");

        const wrapper = input.closest(".form-group");
        if (!wrapper) continue;

        const errorContainer = document.createElement("div");
        errorContainer.className = "form-error";

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

    const div = document.createElement("div");
    div.className = "form-error";
    div.textContent = message;

    container.appendChild(div);
}

function getOrCreateGlobalErrorContainer(form: HTMLFormElement): HTMLElement {
    let container = form.querySelector(".form-global-errors") as HTMLElement | null;

    if (!container) {
        container = document.createElement("div");
        container.className = "form-global-errors";
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
        form.innerHTML = `<div class="form-success">${message}</div>`;
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