<script setup lang="ts">
import { getFormErrors } from '@sproutforms/client';
import { computed } from 'vue';
import { useSproutForm } from '../../context';

const form = useSproutForm();

const errors = computed(() => {
    const errors = getFormErrors(form.definition, form.state.value.errors);
    return form.state.value.submitError ? [form.definition.texts.submitFailed, ...errors] : errors;
});
</script>

<template>
    <div v-if="errors.length > 0" class="form-error" data-sf-global-errors role="alert">
        <div v-for="error in errors" :key="error">{{ error }}</div>
    </div>
</template>
