<script setup lang="ts">
import { useSproutFormContext } from '../../context';

const form = useSproutFormContext();
</script>

<template>
    <div class="form-actions">
        <button type="submit" :disabled="form.state.value.status === 'submitting'">{{ form.definition.submitLabel }}</button>
    </div>
</template>
