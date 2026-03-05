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
            const input = fieldContainer.querySelector("input, textarea, select") as HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement | null;
            const value = input?.value;
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

    const validation = await validateAllFields(form);
    if (!validation) {
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
        applyErrors(form, result.errors || {}, result.values || {});
        return;
    }

    if (result.outcomeType) {
        const handler = window.SproutForms?.outcomeHandlers?.registry?.[result.outcomeType];
        if (handler) {
            await handler(form, result.outcomeData);
            return;
        }
    }
});

document.addEventListener("DOMContentLoaded", () => {
    document.querySelectorAll("form[data-form-ajax]").forEach(initForm);
});

function initForm(form: Element) {
    initFormGuards(form as HTMLFormElement);
    initConditionalFields(form as HTMLFormElement);
    initPageUrl(form as HTMLFormElement);
    initValidation(form as HTMLFormElement);
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

async function validateAllFields(form: HTMLFormElement): Promise<boolean> {
    let isValid = true;
    const groups = form.querySelectorAll("[data-sf-validate]");

    for (const group of groups) {
        const groupEl = group as HTMLElement;
        if (groupEl.classList.contains("sf-hidden") || groupEl.style.display === "none") {
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

    const checkboxes = form.querySelectorAll("input[type='checkbox']");
    checkboxes.forEach(cb => {
        const checkbox = cb as HTMLInputElement;
        values[checkbox.name] = checkbox.checked;
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

function clearErrors(form: HTMLFormElement) {
    form.querySelectorAll(".form-error").forEach(e => e.remove());
    form.querySelectorAll("[aria-invalid]").forEach(el => {
        el.setAttribute("aria-invalid", "false");
    });
}

function applyErrors(form: HTMLFormElement, errors: Record<string, string[]>, values: Record<string, unknown>) {
    for (const key in values) {
        const input = form.querySelector(`[name="${key}"]`) as HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement | null;
        if (input) input.value = String(values[key]);
    }

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