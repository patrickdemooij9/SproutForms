<script setup lang="ts">
import type { DateFieldConfiguration } from '@sproutforms/client';
import { computed } from 'vue';
import { fieldControlProps } from '../../fieldProps';

const props = defineProps(fieldControlProps);
const emit = defineEmits<{ 'update:modelValue': [value: string] }>();

const config = computed(() => (props.field.configuration ?? {}) as Partial<DateFieldConfiguration>);
const includeTime = computed(() => config.value.includeTime === true);

// The configuration's limits are ISO 8601; the input takes "2025-01-31", or "2025-01-31T09:30" with a time
function toInputValue(value: string | null | undefined): string | undefined {
    if (!value) return undefined;
    return value.slice(0, includeTime.value ? 16 : 10);
}
</script>

<template>
    <input
        :id="id"
        :type="includeTime ? 'datetime-local' : 'date'"
        class="form-control"
        :name="path"
        :value="(modelValue as string | undefined) ?? ''"
        :min="toInputValue(config.min)"
        :max="toInputValue(config.max)"
        :required="required"
        :disabled="disabled"
        :aria-invalid="invalid"
        :aria-describedby="describedBy"
        @input="emit('update:modelValue', ($event.target as HTMLInputElement).value)"
    />
</template>
