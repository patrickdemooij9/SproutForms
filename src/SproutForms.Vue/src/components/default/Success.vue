<script setup lang="ts">
defineProps<{
    // The outcome's message: HTML an editor wrote in the CMS, shown as the Razor forms show it
    message?: string;
    // The form's thank-you text, when the outcome has no message
    text?: string;
}>();
</script>

<template>
    <div v-if="message" class="form-success" data-sf-success role="status" v-html="message" />
    <div v-else class="form-success" data-sf-success role="status">{{ text }}</div>
</template>
