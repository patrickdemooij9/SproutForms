<script setup lang="ts">
import { isFieldRequired, isFieldVisible, type FormClientField } from '@sproutforms/client';
import { computed } from 'vue';
import { useSproutFormContext } from '../../context';

const props = defineProps<{
    field: FormClientField;
}>();

const form = useSproutFormContext();
const control = form.resolveField(props.field);
if (!control) {
    console.warn(`SproutForms: no component for field type "${props.field.type}" (field "${props.field.alias}"). Register one with registerField.`);
}

const path = props.field.alias;
const id = form.getFieldId(path);
const errorId = `${id}-error`;

const visible = computed(() => isFieldVisible(props.field, form.state.value.values, form.state.value.variables));
const required = computed(() => isFieldRequired(props.field, form.state.value.values, form.state.value.variables));
const errors = computed(() => form.state.value.errors[path] ?? []);
const value = computed(() => form.state.value.values[path]);
const submitting = computed(() => form.state.value.status === 'submitting');
</script>

<template>
    <div
        v-if="control"
        :class="['form-group', { 'has-error': errors.length > 0 }]"
        :data-sf-field-id="field.alias"
        :data-sf-field-type="field.type"
        :hidden="!visible || undefined"
    >
        <label v-if="!field.rendersOwnLabel && field.type !== 'hidden'" :for="id">
            {{ field.label }}
            <span v-if="required" class="required">*</span>
        </label>

        <component
            :is="control"
            :id="id"
            :field="field"
            :model-value="value"
            :path="path"
            :required="required"
            :invalid="errors.length > 0"
            :errors="errors"
            :disabled="submitting"
            :described-by="errors.length > 0 ? errorId : undefined"
            @update:model-value="form.engine.setValue(path, $event)"
        />

        <div v-if="errors.length > 0" :id="errorId" class="form-error" data-sf-error role="alert">
            <div v-for="error in errors" :key="error">{{ error }}</div>
        </div>
    </div>
</template>
