<script setup lang="ts">
import type { FileFieldConfiguration } from '@sproutforms/client';
import { computed } from 'vue';
import { fieldControlProps } from '../../fieldProps';

const props = defineProps(fieldControlProps);
// The chosen File, which the client sends as an upload; undefined when the visitor clears it
const emit = defineEmits<{ 'update:modelValue': [value: File | undefined] }>();

// Only a hint for the file picker: the server checks the extension and size
const accept = computed(() => ((props.field.configuration ?? {}) as Partial<FileFieldConfiguration>).allowedExtensions?.join(',') || undefined);
</script>

<!-- A file input's value can't be set, so it shows what the visitor chose itself -->
<template>
    <input
        :id="id"
        type="file"
        class="form-control"
        :name="path"
        :accept="accept"
        :required="required"
        :disabled="disabled"
        :aria-invalid="invalid"
        :aria-describedby="describedBy"
        @change="emit('update:modelValue', ($event.target as HTMLInputElement).files?.[0] ?? undefined)"
    />
</template>
