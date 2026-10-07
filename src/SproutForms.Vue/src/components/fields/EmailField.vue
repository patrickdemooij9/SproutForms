<script setup lang="ts">
import type { EmailFieldConfiguration } from '@sproutforms/client';
import { computed } from 'vue';
import { fieldControlProps } from '../../fieldProps';

const props = defineProps(fieldControlProps);
const emit = defineEmits<{ 'update:modelValue': [value: string] }>();

const config = computed(() => (props.field.configuration ?? {}) as EmailFieldConfiguration);
</script>

<template>
    <input
        :id="id"
        type="email"
        class="form-control"
        autocomplete="email"
        :name="path"
        :value="modelValue ?? ''"
        :placeholder="config.placeholder ?? undefined"
        :required="required"
        :disabled="disabled"
        :aria-invalid="invalid"
        :aria-describedby="describedBy"
        @input="emit('update:modelValue', ($event.target as HTMLInputElement).value)"
    />
</template>
