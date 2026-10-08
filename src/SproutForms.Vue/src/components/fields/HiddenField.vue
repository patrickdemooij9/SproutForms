<script setup lang="ts">
import { fieldControlProps } from '../../fieldProps';

// The value is in the form's state, from the field's default or the form's initial values; the input only marks the field
defineProps(fieldControlProps);
</script>

<template>
    <input :id="id" type="hidden" :name="path" :value="modelValue ?? ''" />
</template>
