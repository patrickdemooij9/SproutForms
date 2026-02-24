window.sproutForms = {
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
    }
};

document.addEventListener("submit", async function (e) {
    const form = e.target;

    if (!form.matches("[data-form-ajax]")) return;

    e.preventDefault();

    const formData = new FormData(form);

    const submissionGuards = getSubmissionGuards(form);
    try {
        for (const guardDef of submissionGuards) {
            const guard = window.sproutForms.submissionGuard?.registry?.[guardDef.alias];
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
}

function initPageUrl(form) {
    const pageUrlInput = form.querySelector('[data-sf-page-url="true"]');
    if (pageUrlInput) {
        pageUrlInput.value = window.location.href;
    }
}

function initConditionalFields(form) {
    const formFields = form.querySelectorAll("[data-field-conditions]");
    
    formFields.forEach(wrapper => {
        const fieldAlias = wrapper.getAttribute("data-field-id");
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
        
        const isVisible = window.sproutForms.conditions.evaluate(visibilityCondition, formValues);
        
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
        
        if (isVisible && requiredCondition && window.sproutForms.conditions.evaluate(requiredCondition, formValues)) {
            const input = wrapper.querySelector("input, select, textarea");
            if (input) {
                input.setAttribute("data-conditional-required", "true");
                input.setAttribute("required", "");
            }
        }
    });
}

sproutForms.submissionGuard.register("recaptchaV3", {
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
        const input = form.querySelector(`[data-field-id="${fieldId}"]`)
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
        const guard = window.sproutForms.submissionGuard?.registry?.[guardDef.alias];
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
