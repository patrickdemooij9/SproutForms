<script setup lang="ts">
import type { FormClientField } from '@sproutforms/client';
import { useField } from '../../useField';

const props = defineProps<{
    field: FormClientField;
}>();

const { id, errorId, visible, required, errors, invalid, control, controlProps } = useField(() => props.field);
if (!control.value) {
    console.warn(`SproutForms: no component for field type "${props.field.type}" (field "${props.field.alias}"). Register one with registerField.`);
}
</script>

<template>
    <div
        v-if="control"
        :class="['form-group', { 'has-error': invalid }]"
        :data-sf-field-id="field.alias"
        :data-sf-field-type="field.type"
        :hidden="!visible || undefined"
    >
        <label v-if="!field.rendersOwnLabel && field.type !== 'hidden'" :for="id">
            {{ field.label }}
            <span v-if="required" class="required">*</span>
        </label>

        <component :is="control" v-bind="controlProps" />

        <div v-if="invalid" :id="errorId" class="form-error" data-sf-error role="alert">
            <div v-for="error in errors" :key="error">{{ error }}</div>
        </div>
    </div>
</template>
