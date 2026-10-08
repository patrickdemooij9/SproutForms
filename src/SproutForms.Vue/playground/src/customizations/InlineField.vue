<script setup lang="ts">
import type { FormClientField } from '@sproutforms/client';
import { useField } from '@sproutforms/vue';

// A field wrapper with the label next to the control and the error under it, as a theme can replace any part of the form
const props = defineProps<{ field: FormClientField }>();

const { id, errorId, visible, required, errors, invalid, control, controlProps } = useField(() => props.field);
</script>

<template>
    <div
        class="inline-field"
        :class="{ 'has-error': invalid, 'inline-field-own-label': field.rendersOwnLabel }"
        :data-sf-field-id="field.alias"
        :data-sf-field-type="field.type"
        :hidden="!visible || undefined"
    >
        <label v-if="!field.rendersOwnLabel && field.type !== 'hidden'" :for="id">{{ field.label }}{{ required ? ' *' : '' }}</label>
        <component :is="control" v-bind="controlProps" />
        <small v-if="invalid" :id="errorId" class="inline-field-error">{{ errors[0] }}</small>
    </div>
</template>

<style scoped>
.inline-field {
    display: grid;
    grid-template-columns: 8rem 1fr;
    align-items: center;
    gap: 0.25rem 0.75rem;
}

/* A control with its own label, such as a checkbox, takes the label's place */
.inline-field-own-label > :first-child {
    grid-column: 2;
}

.inline-field > label {
    font-weight: 600;
}

.inline-field-error {
    grid-column: 2;
    color: var(--sf-color-error);
}
</style>
