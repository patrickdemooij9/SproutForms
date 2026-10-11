<script setup lang="ts">
import type { FormVariables, HeadlessOutcome } from '@sproutforms/client';
import { SproutForm } from '@sproutforms/vue';
import { ref } from 'vue';
import CountedTextarea from './customizations/CountedTextarea.vue';

const params = new URLSearchParams(window.location.search);
const alias = ref(params.get('form') ?? 'aiTestHeadlessBasics');
const theme = ref(params.get('theme') ?? 'default');
const overrideMessage = ref(params.get('override') !== 'false');
const log = ref<string[]>([]);

function onSubmitted(outcome: HeadlessOutcome | null, variables: FormVariables) {
    log.value.unshift(`submitted: ${JSON.stringify(outcome)}, variables: ${JSON.stringify(variables)}`);
}

function onError(error: unknown) {
    log.value.unshift(`error: ${String(error)}`);
}
</script>

<template>
    <main>
        <h1>SproutForms Vue playground</h1>
        <form class="settings" @submit.prevent>
            <label>Form <input v-model.lazy="alias" /></label>
            <label>
                Theme
                <select v-model="theme">
                    <option value="default">default</option>
                    <option value="inline">inline (replaces the field wrapper)</option>
                </select>
            </label>
            <label><input v-model="overrideMessage" type="checkbox" /> Count the characters of the "message" field (an alias override)</label>
        </form>

        <!-- A new key starts the form over, as a theme or override applies when a form is created -->
        <SproutForm
            :key="`${alias}|${theme}|${overrideMessage}`"
            :alias="alias"
            :theme="theme"
            :fields="overrideMessage ? { message: CountedTextarea } : undefined"
            @submitted="onSubmitted"
            @error="onError"
        />

        <h2>Events</h2>
        <pre class="log">{{ log.join('\n') || 'None yet' }}</pre>
    </main>
</template>
