window.sproutForms = {
    submissionGuard: {
        registry: {},
        register(alias, guard) {
            this.registry[alias] = guard;
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
    document.querySelectorAll("form[data-form-ajax]").forEach(initFormGuards);
});

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
