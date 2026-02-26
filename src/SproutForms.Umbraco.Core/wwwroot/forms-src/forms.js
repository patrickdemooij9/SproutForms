window.SproutForms = {
    submissionGuard: {
        registry: {},
        register(alias, guard) {
            this.registry[alias] = guard;
        }
    },
    conditions: {
        registry: null,
        init(form) {
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
        evaluate(fieldConditions, formValues) {
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
                        return parseFloat(fieldValue) > parseFloat(targetValue);
                    case "LessThan":
                        return parseFloat(fieldValue) < parseFloat(targetValue);
                    case "IsEmpty":
                        return !fieldValue || String(fieldValue).trim() === "";
                    case "IsNotEmpty":
                        return fieldValue && String(fieldValue).trim() !== "";
                    case "MatchesRegex":
                        try {
                            return new RegExp(targetValue).test(String(fieldValue || ""));
                        } catch {
                            return false;
                        }
                    case "DoesNotMatchRegex":
                        try {
                            return !new RegExp(targetValue).test(String(fieldValue || ""));
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
        registry: {},

        register(alias, validator) {
            this.registry[alias] = validator;
        },

        async validateField(fieldContainer) {
            const rules = fieldContainer.dataset.sfValidate?.split(",") || [];
            const value = fieldContainer.querySelector("input, textarea, select")?.value; //TODO: Make this more extendable

            for (let rule of rules) {
                const validator = this.registry[rule.trim()];
                if (!validator) continue;

                const isValid = await validator(value, fieldContainer.dataset);

                if (!isValid) {
                    const capitalizedType = rule.charAt(0).toUpperCase() + rule.slice(1)
                    return {
                        valid: false,
                        rule,
                        message: fieldContainer.dataset[`sf${capitalizedType}Message`]
                    };
                }
            }

            return { valid: true };
        }
    }
};

document.addEventListener("submit", async function (e) {
    const form = e.target;

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
            const guard = window.SproutForms.submissionGuard?.registry?.[guardDef.alias];
            if (!guard) continue;

            if (guard.beforeSubmit) {
                await guard.beforeSubmit(form, guardDef.settings, formData);
            }
        }
    } catch (err) {
        applyGlobalError(form, err.message || "Something went wrong. Please try again.");
        return;
    }

    const response = await fetch(form.action, {
        method: "POST",
        headers: {
            "X-Requested-With": "XMLHttpRequest"
        },
        body: formData
    });

    const result = await response.json();

    clearErrors(form);

    if (!response.ok) {
        applyErrors(form, result.errors, result.values);
        return;
    }

    if (result.successMessage) {
        form.innerHTML =
            `<div class="form-success">${result.successMessage}</div>`;
        return;
    }

    if (result.redirectUrl) {
        window.location.href = result.redirectUrl;
    }
});

document.addEventListener("DOMContentLoaded", () => {
    document.querySelectorAll("form[data-form-ajax]").forEach(initForm);
});

function initForm(form) {
    initFormGuards(form);
    initConditionalFields(form);
    initPageUrl(form);
    initValidation(form);
}

function initPageUrl(form) {
    const pageUrlInput = form.querySelector('[data-sf-page-url="true"]');
    if (pageUrlInput) {
        pageUrlInput.value = window.location.href;
    }
}

function initValidation(form) {
    const validateGroups = form.querySelectorAll("[data-sf-validate]");

    validateGroups.forEach(group => {
        const inputs = group.querySelectorAll("input, select, textarea");

        inputs.forEach(input => {
            input.addEventListener("blur", async () => await validateGroup(group));
        });

        inputs.forEach(input => {
            input.addEventListener("input", () => clearFieldError(group));
        });
    });
}

async function validateGroup(group) {
    const result = await window.SproutForms.validation.validateField(group);

    if (!result.valid) {
        showFieldError(group, result.message);
        return false;
    }

    clearFieldError(group);
    return true;
}

function showFieldError(group, message) {
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

function clearFieldError(group) {
    const inputs = group.querySelectorAll("input, select, textarea");
    inputs.forEach(input => input.setAttribute("aria-invalid", "false"));

    const existingError = group.querySelector(".form-error");
    if (existingError) {
        existingError.remove();
    }
}

async function validateAllFields(form) {
    let isValid = true;
    const groups = form.querySelectorAll("[data-sf-validate]");

    for (const group of groups) {
        if (group.classList.contains("sf-hidden") || group.style.display === "none") {
            continue;
        }

        const result = await validateGroup(group);
        if (!result) {
            isValid = false;
        }
    }

    return isValid;
}

function initConditionalFields(form) {
    const formFields = form.querySelectorAll("[data-field-conditions]");

    formFields.forEach(wrapper => {
        const fieldAlias = wrapper.getAttribute("data-sf-field-id");
        const conditionsRaw = wrapper.getAttribute("data-field-conditions");

        if (!conditionsRaw || !fieldAlias) return;

        try {
            const conditions = JSON.parse(conditionsRaw);
            wrapper.setAttribute("data-condition-field", fieldAlias);
            wrapper.conditions = conditions;
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

function getFormValues(form) {
    const values = {};
    const formData = new FormData(form);
    for (const [key, value] of formData.entries()) {
        values[key] = value;
    }

    const checkboxes = form.querySelectorAll("input[type='checkbox']");
    checkboxes.forEach(cb => {
        values[cb.name] = cb.checked;
    });

    return values;
}

function evaluateAllConditions(form) {
    const formValues = getFormValues(form);
    const fields = form.querySelectorAll("[data-condition-field]");

    fields.forEach(wrapper => {
        const conditions = wrapper.conditions;
        if (!conditions) return;

        const visibilityCondition = conditions.visibility;
        const requiredCondition = conditions.required;

        const isVisible = window.SproutForms.conditions.evaluate(visibilityCondition, formValues);

        const parentCol = wrapper.closest(".form-col");

        if (isVisible) {
            wrapper.style.display = "";
            wrapper.removeAttribute("hidden");
            wrapper.classList.remove("sf-hidden");
            if (parentCol) {
                parentCol.style.display = "";
                parentCol.classList.remove("sf-hidden");
            }
        } else {
            wrapper.style.display = "none";
            wrapper.setAttribute("hidden", "");
            wrapper.classList.add("sf-hidden");
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
            const input = wrapper.querySelector("input, select, textarea");
            if (input) {
                input.setAttribute("data-conditional-required", "true");
                input.setAttribute("required", "");
            }
        }
    });
}

SproutForms.submissionGuard.register("recaptchaV3", {
    async load(form, settings) {
        if (window.grecaptcha) return;

        await loadScript(
            `https://www.google.com/recaptcha/api.js?render=${settings.siteKey}`
        );
    },

    async beforeSubmit(form, settings, payload) {
        const token = await grecaptcha.execute(settings.siteKey, {
            action: settings.action || "submit"
        });

        payload.append("g-recaptcha-response", token);
    }
});

SproutForms.validation.register("required", async (value, options) => {
    if (!value) return false;
    return value.trim().length > 0;
});

SproutForms.validation.register("minLength", async (value, options) => {
    if (!value) return true;
    return value.length >= parseInt(options.sfMinLength);
});

SproutForms.validation.register("maxLength", async (value, options) => {
    if (!value) return true;
    return value.length <= parseInt(options.sfMaxLength);
});

SproutForms.validation.register("sameAs", async (value, options, context) => {
    const other = context.querySelector(`[name="${options.sfOther}"]`);
    return other && value === other.value;
});

SproutForms.validation.register("regex", async (value, options) => {
    if (!value) return true;

    const pattern = options.sfRegex;
    if (!pattern) return true;

    let regex;

    try {
        regex = new RegExp(pattern);
    } catch (e) {
        console.warn("Invalid regex pattern:", pattern);
        return false; // fail-safe
    }

    return regex.test(value);
});

SproutForms.validation.register("minDate", async (value, options) => {
    if (!value) return true;

    const minDateValue = options.sfMinDate;
    if (!minDateValue) return true;

    const inputDate = new Date(value);
    const minDate = new Date(minDateValue);

    if (isNaN(inputDate) || isNaN(minDate)) {
        console.warn("Invalid date value in minDate validator.");
        return true;
    }

    if (inputDate >= minDate) {
        return true;
    }

    return false;
});

SproutForms.validation.register("maxDate", async (value, options) => {
    if (!value) return true;

    const minDateValue = options.sfMaxDate;
    if (!minDateValue) return true;

    const inputDate = new Date(value);
    const minDate = new Date(minDateValue);

    if (isNaN(inputDate) || isNaN(minDate)) {
        console.warn("Invalid date value in minDate validator.");
        return true;
    }

    if (inputDate < minDate) {
        return true;
    }

    return false;
});

function clearErrors(form) {
    form.querySelectorAll(".form-error").forEach(e => e.remove());
    form.querySelectorAll("[aria-invalid]").forEach(el => {
        el.setAttribute("aria-invalid", "false");
    });
}

function applyErrors(form, errors, values) {
    for (const key in values) {
        const input = form.querySelector(`[name="${key}"]`);
        if (input) input.value = values[key];
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
        };

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

function applyGlobalError(form, message) {
    const container = getOrCreateGlobalErrorContainer(form);

    const div = document.createElement("div");
    div.className = "form-error";
    div.textContent = message;

    container.appendChild(div);
}

function getOrCreateGlobalErrorContainer(form) {
    let container = form.querySelector(".form-global-errors");

    if (!container) {
        container = document.createElement("div");
        container.className = "form-global-errors";
        container.setAttribute("role", "alert");
        container.setAttribute("aria-live", "assertive");
        form.prepend(container);
    }

    return container;
}

async function initFormGuards(form) {
    const submissionGuards = getSubmissionGuards(form);

    for (const guardDef of submissionGuards) {
        const guard = window.SproutForms.submissionGuard?.registry?.[guardDef.alias];
        if (!guard || !guard.load) continue;

        try {
            await guard.load(form, guardDef.settings);
        } catch (err) {
            console.error(`Enhancer '${guardDef.alias}' failed to load`, err);
            applyGlobalError(
                form,
                "This form could not be initialized correctly. Please try again later."
            );
        }
    }
}

function loadScript(src) {
    return new Promise((resolve, reject) => {
        const s = document.createElement("script");
        s.src = src;
        s.async = true;
        s.onload = resolve;
        s.onerror = reject;
        document.head.appendChild(s);
    });
}

function getSubmissionGuards(form) {
    const raw = form.getAttribute("data-submission-guards");
    if (!raw) return [];

    try {
        return JSON.parse(raw);
    } catch {
        return [];
    }
}
