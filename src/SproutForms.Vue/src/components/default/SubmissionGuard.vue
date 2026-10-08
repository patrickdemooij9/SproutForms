<script setup lang="ts">
import { computed } from 'vue';
import { useSproutFormContext } from '../../context';

const form = useSproutFormContext();

// The honeypot's input, by the name the server checks; other guards, such as reCAPTCHA, need no markup
const honeypotName = computed(() => {
    const guard = form.definition.submissionGuard;
    if (guard?.alias !== 'honeypot') return undefined;
    const fieldName = (guard.settings as Record<string, unknown> | undefined)?.fieldName;
    return typeof fieldName === 'string' && fieldName ? fieldName : undefined;
});

function onInput(event: Event): void {
    form.engine.setValue(honeypotName.value!, (event.target as HTMLInputElement).value);
}
</script>

<template>
    <!-- Hidden with an inline style, so it stays hidden on sites that don't use the default theme (Guards/Honeypot.cshtml) -->
    <div v-if="honeypotName" aria-hidden="true" style="position:absolute;left:-10000px;width:1px;height:1px;overflow:hidden;">
        <label>
            Leave this field empty
            <input
                type="text"
                :name="honeypotName"
                :value="String(form.state.value.values[honeypotName] ?? '')"
                tabindex="-1"
                autocomplete="off"
                @input="onInput"
            >
        </label>
    </div>
</template>
