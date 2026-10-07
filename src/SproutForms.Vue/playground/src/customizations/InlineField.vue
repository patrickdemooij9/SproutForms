<script setup lang="ts">
import { isFieldRequired, isFieldVisible, type FormClientField } from '@sproutforms/client';
import { useSproutFormContext } from '@sproutforms/vue';
import { computed } from 'vue';

// A field wrapper with the label next to the control and the error under it, as a theme can replace any part of the form
const props = defineProps<{ field: FormClientField }>();

const form = useSproutFormContext();
const control = form.resolveField(props.field);
const id = form.getFieldId(props.field.alias);

const visible = computed(() => isFieldVisible(props.field, form.state.value.values, form.state.value.variables));
const required = computed(() => isFieldRequired(props.field, form.state.value.values, form.state.value.variables));
const errors = computed(() => form.state.value.errors[props.field.alias] ?? []);
</script>

<template>
    <div
        class="inline-field"
        :class="{ 'has-error': errors.length > 0, 'inline-field-own-label': field.rendersOwnLabel }"
        :data-sf-field-id="field.alias"
        :data-sf-field-type="field.type"
        :hidden="!visible || undefined"
    >
        <label v-if="!field.rendersOwnLabel && field.type !== 'hidden'" :for="id">{{ field.label }}{{ required ? ' *' : '' }}</label>
        <component
            :is="control"
            :id="id"
            :field="field"
            :model-value="form.state.value.values[field.alias]"
            :path="field.alias"
            :required="required"
            :invalid="errors.length > 0"
            :errors="errors"
            @update:model-value="form.engine.setValue(field.alias, $event)"
        />
        <small v-if="errors.length > 0" class="inline-field-error">{{ errors[0] }}</small>
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
