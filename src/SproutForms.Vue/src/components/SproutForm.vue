<script setup lang="ts">
import type { FormClientModel, FormValues, FormVariables, HeadlessOutcome } from '@sproutforms/client';
import { computed, watch, type Component } from 'vue';
import { useSproutFormsPlugin } from '../plugin';
import SproutFormView from './SproutFormView.vue';

const props = defineProps<{
    // The form's id or alias, to load its published definition
    alias?: string;
    // A definition you loaded yourself, instead of an alias
    definition?: FormClientModel;
    // A theme registered on the plugin; the default theme when left out
    theme?: string;
    // Controls for this form's fields by alias, over their field type's control
    fields?: Record<string, Component>;
    initialValues?: FormValues;
}>();

const emit = defineEmits<{
    submitted: [outcome: HeadlessOutcome | null, variables: FormVariables];
    // The definition couldn't be loaded, or the submit or a page check failed other than on validation
    error: [error: unknown];
    pagechange: [index: number, previousIndex: number];
}>();

const sproutForms = useSproutFormsPlugin();

// Only loaded when no definition is passed in; a component can't call the loader conditionally later
const request = props.definition ? undefined : sproutForms.loadDefinition(() => {
    if (!props.alias) throw new Error('<SproutForm> needs an alias or a definition.');
    return props.alias;
});

const definition = computed(() => props.definition ?? request?.definition.value);

if (request) {
    watch(request.error, error => { if (error) emit('error', error); });
}
</script>

<template>
    <!-- A new version of the form starts over, with a new engine -->
    <SproutFormView
        v-if="definition"
        :key="definition.versionId"
        :definition="definition"
        :theme="theme"
        :fields="fields"
        :initial-values="initialValues"
        @submitted="(outcome, variables) => emit('submitted', outcome, variables)"
        @error="emit('error', $event)"
        @pagechange="(index, previousIndex) => emit('pagechange', index, previousIndex)"
    />
</template>
