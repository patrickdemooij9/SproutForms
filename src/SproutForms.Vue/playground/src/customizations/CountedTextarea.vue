<script setup lang="ts">
import { fieldControlProps } from '@sproutforms/vue';
import { computed } from 'vue';

// A control for one field of one form, passed to <SproutForm :fields>: a textarea that counts what was typed
const props = defineProps(fieldControlProps);
const emit = defineEmits<{ 'update:modelValue': [value: string] }>();

const text = computed(() => typeof props.modelValue === 'string' ? props.modelValue : '');
</script>

<template>
    <div class="counted-textarea">
        <textarea
            :id="id"
            class="form-control"
            rows="4"
            :value="text"
            :disabled="disabled"
            :aria-invalid="invalid"
            :aria-describedby="describedBy"
            @input="emit('update:modelValue', ($event.target as HTMLTextAreaElement).value)"
        />
        <small class="counted-textarea-count">{{ text.length }} characters</small>
    </div>
</template>

<style scoped>
.counted-textarea {
    display: flex;
    flex-direction: column;
}

.counted-textarea-count {
    color: var(--sf-color-text-muted);
    text-align: right;
}
</style>
