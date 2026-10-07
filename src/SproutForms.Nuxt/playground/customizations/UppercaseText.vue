<script setup lang="ts">
// A control for one field of one form, passed to <SproutForm :fields>: a text input that keeps what was typed in capitals
defineProps(fieldControlProps);
const emit = defineEmits<{ 'update:modelValue': [value: string] }>();
</script>

<template>
    <input
        :id="id"
        type="text"
        class="form-control"
        :value="(modelValue as string | undefined) ?? ''"
        :disabled="disabled"
        :aria-invalid="invalid"
        :aria-describedby="describedBy"
        @input="emit('update:modelValue', ($event.target as HTMLInputElement).value.toUpperCase())"
    />
</template>
