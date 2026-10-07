<script setup lang="ts">
import type { TextAreaFieldConfiguration } from '@sproutforms/client';
import { computed } from 'vue';
import { fieldControlProps } from '../../fieldProps';

const props = defineProps(fieldControlProps);
const emit = defineEmits<{ 'update:modelValue': [value: string] }>();

const config = computed(() => (props.field.configuration ?? {}) as Partial<TextAreaFieldConfiguration>);
</script>

<template>
    <textarea
        :id="id"
        class="form-control"
        :name="path"
        :value="(modelValue as string | undefined) ?? ''"
        :rows="config.rows ?? undefined"
        :maxlength="config.maxLength ?? undefined"
        :required="required"
        :disabled="disabled"
        :aria-invalid="invalid"
        :aria-describedby="describedBy"
        @input="emit('update:modelValue', ($event.target as HTMLTextAreaElement).value)"
    />
</template>
