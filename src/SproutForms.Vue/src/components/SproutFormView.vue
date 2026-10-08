<script setup lang="ts">
import { createFormEngine, handleOutcome, type FormClientField, type FormClientModel, type FormValues, type HeadlessOutcome } from '@sproutforms/client';
import { nextTick, onMounted, onScopeDispose, provide, ref, shallowRef, useId, watch, type Component } from 'vue';
import { sproutFormKey, type FormSuccess, type SproutFormContext } from '../context';
import { useSproutFormsPlugin } from '../plugin';
import { defaultThemeName, type FormComponentName } from '../themes';

const props = defineProps<{
    definition: FormClientModel;
    // A theme registered on the plugin; the default theme when left out
    theme?: string;
    // Controls for this form's fields by alias, over their field type's control
    fields?: Record<string, Component>;
    initialValues?: FormValues;
}>();

const emit = defineEmits<{
    // The form is submitted; outcome is null when the server had none to give
    submitted: [outcome: HeadlessOutcome | null];
    // The submit, or checking a page with the server, failed other than on validation, such as a network error
    error: [error: unknown];
    // The visitor went to another page; indexes are those of the definition's pages
    pagechange: [index: number, previousIndex: number];
}>();

const sproutForms = useSproutFormsPlugin();
const theme = props.theme ?? defaultThemeName;
const idPrefix = useId();

const engine = createFormEngine(props.definition, {
    client: sproutForms.client,
    initialValues: props.initialValues,
    validators: sproutForms.validators,
    guards: sproutForms.guards
});
const state = shallowRef(engine.getState());
onScopeDispose(engine.subscribe(next => { state.value = next; }));

const success = ref<FormSuccess>();

function getFieldId(path: string): string {
    return `${idPrefix}-${path}`;
}

function focusFirstError(): void {
    const path = Object.keys(state.value.errors)[0];
    if (path) document.getElementById(getFieldId(path))?.focus();
}

async function next(): Promise<void> {
    let moved;
    try {
        moved = await engine.next();
    } catch (error) {
        emit('error', error);
        return;
    }

    if (!moved) {
        await nextTick();
        focusFirstError();
    }
}

function previous(): void {
    engine.previous();
}

async function submit(): Promise<void> {
    // Enter in a field submits the form, also on a page before the last
    if (!engine.isLastPage()) return next();

    let result;
    try {
        result = await engine.submit();
    } catch (error) {
        emit('error', error);
        return;
    }

    if (!result.ok) {
        await nextTick();
        focusFirstError();
        return;
    }

    emit('submitted', result.outcome);
    const handled = await handleOutcome(result.outcome, {
        definition: props.definition,
        showMessage: message => { success.value = message ? { message } : { text: props.definition.texts.submitSucceeded }; },
        navigate: url => sproutForms.navigate(url)
    }, sproutForms.outcomeHandlers);
    if (!handled) success.value = { text: props.definition.texts.submitSucceeded };
}

const context: SproutFormContext = {
    definition: props.definition,
    engine,
    state,
    success,
    theme,
    getFieldId,
    resolveField: (field: FormClientField) => props.fields?.[field.alias] ?? sproutForms.resolveField(field.type, theme),
    resolveComponent: (name: FormComponentName) => sproutForms.resolveComponent(name, theme),
    submit,
    next,
    previous
};
provide(sproutFormKey, context);

watch(() => state.value.pageIndex, (index, previousIndex) => emit('pagechange', index, previousIndex));

onMounted(() => {
    // A guard that can't load, such as reCAPTCHA being blocked, fails the submit instead, with the server's error
    engine.loadSubmissionGuard().catch(() => undefined);
});

const Form = context.resolveComponent('Form');

defineExpose({ engine, submit, next, previous });
</script>

<template>
    <component :is="Form" />
</template>
