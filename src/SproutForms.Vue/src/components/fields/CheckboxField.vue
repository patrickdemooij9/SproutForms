<script setup lang="ts">
import { fieldControlProps } from '../../fieldProps';

defineProps(fieldControlProps);
const emit = defineEmits<{ 'update:modelValue': [value: boolean] }>();
</script>

<!-- Renders its own label, next to the box, so the wrapper leaves its label out -->
<template>
    <label class="checkbox form-choice">
        <input
            :id="id"
            type="checkbox"
            :name="path"
            value="true"
            :checked="modelValue === true || modelValue === 'true'"
            :required="required"
            :disabled="disabled"
            :aria-invalid="invalid"
            :aria-describedby="describedBy"
            @change="emit('update:modelValue', ($event.target as HTMLInputElement).checked)"
        />
        <span>
            {{ field.label }}
            <span v-if="required" class="required">*</span>
        </span>
    </label>
</template>
