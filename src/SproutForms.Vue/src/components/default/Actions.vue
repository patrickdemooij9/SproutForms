<script setup lang="ts">
import { useSproutForm } from '../../context';

const form = useSproutForm();
</script>

<template>
    <div class="form-actions">
        <button type="submit" :disabled="form.state.value.status === 'submitting'">{{ form.definition.submitLabel }}</button>
    </div>
</template>
