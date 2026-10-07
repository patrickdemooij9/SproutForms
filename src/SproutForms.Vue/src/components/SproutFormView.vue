<script setup lang="ts">
import { createFormEngine, handleOutcome, type FormClientField, type FormClientModel, type FormValues, type HeadlessOutcome } from '@sproutforms/client';
import { nextTick, onMounted, onScopeDispose, provide, ref, shallowRef, useId, type Component } from 'vue';
import { sproutFormKey, type FormSuccess, type SproutFormContext } from '../context';
import { useSproutForms } from '../plugin';
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
    // The submit failed other than on validation, such as a network error
    error: [error: unknown];
}>();

const sproutForms = useSproutForms();
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

async function submit(): Promise<void> {
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
    submit
};
provide(sproutFormKey, context);

onMounted(() => {
    // A guard that can't load, such as reCAPTCHA being blocked, fails the submit instead, with the server's error
    engine.loadSubmissionGuard().catch(() => undefined);
});

const Form = context.resolveComponent('Form');

defineExpose({ engine, submit });
</script>

<template>
    <component :is="Form" />
</template>
