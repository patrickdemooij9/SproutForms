<script setup lang="ts">
import type { TextFieldConfiguration } from '@sproutforms/client';
import { computed } from 'vue';
import { fieldControlProps } from '../../fieldProps';

const props = defineProps(fieldControlProps);
const emit = defineEmits<{ 'update:modelValue': [value: string] }>();

const config = computed(() => (props.field.configuration ?? {}) as TextFieldConfiguration);
</script>

<template>
    <input
        :id="id"
        type="text"
        class="form-control"
        :name="path"
        :value="modelValue ?? ''"
        :placeholder="config.placeholder ?? undefined"
        :maxlength="config.maxLength ?? undefined"
        :required="required"
        :disabled="disabled"
        :aria-invalid="invalid"
        :aria-describedby="describedBy"
        @input="emit('update:modelValue', ($event.target as HTMLInputElement).value)"
    />
</template>
