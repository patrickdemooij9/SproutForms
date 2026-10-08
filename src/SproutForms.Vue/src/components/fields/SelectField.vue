<script setup lang="ts">
import type { OptionsFieldConfiguration } from '@sproutforms/client';
import { computed } from 'vue';
import { fieldControlProps } from '../../fieldProps';

const props = defineProps(fieldControlProps);
const emit = defineEmits<{ 'update:modelValue': [value: string] }>();

const options = computed(() => ((props.field.configuration ?? {}) as Partial<OptionsFieldConfiguration>).options ?? []);
</script>

<template>
    <select
        :id="id"
        class="form-control"
        :name="path"
        :value="modelValue ?? ''"
        :required="required"
        :disabled="disabled"
        :aria-invalid="invalid"
        :aria-describedby="describedBy"
        @change="emit('update:modelValue', ($event.target as HTMLSelectElement).value)"
    >
        <option value="">Select an option</option>
        <option v-for="option in options" :key="option.value" :value="option.value">{{ option.label }}</option>
    </select>
</template>
