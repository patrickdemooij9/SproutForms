document.addEventListener("submit", async function (e) {
    const form = e.target;

    if (!form.matches("[data-form-ajax]")) return;

    e.preventDefault();

    const formData = new FormData(form);

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

        if (!input) continue;

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
