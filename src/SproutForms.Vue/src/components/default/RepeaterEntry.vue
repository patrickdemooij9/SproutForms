<script setup lang="ts">
import { getEntryTitle, type FormClientField, type RepeaterFieldConfiguration } from '@sproutforms/client';
import { computed } from 'vue';
import { useSproutFormContext } from '../../context';
import { provideEntryScope } from '../../scope';

const props = defineProps<{
    // The field group
    field: FormClientField;
    // The group's path, such as "people"
    path: string;
    index: number;
    // False while the group has no more entries than its minimum
    removable: boolean;
}>();

// The repeater removes the entry, so it keeps track of which entry is which
const emit = defineEmits<{ remove: [] }>();

const form = useSproutFormContext();
const Rows = form.resolveComponent('Rows');

// The fields rendered in here are this entry's: their paths start with "people[0]." and their conditions see the entry
const scope = provideEntryScope(() => props.path, () => props.index);

const title = computed(() => getEntryTitle(props.field, props.index));
const removeLabel = computed(() => (props.field.configuration as Partial<RepeaterFieldConfiguration> | null)?.removeLabel);
</script>

<template>
    <fieldset class="form-repeater-entry" data-sf-repeater-entry :data-sf-entry-prefix="scope.prefix.value">
        <legend class="form-repeater-entry-title" data-sf-entry-title>{{ title }}</legend>
        <component :is="Rows" :rows="field.rows ?? []" />
        <button
            type="button"
            class="form-repeater-remove"
            data-sf-repeater-remove
            :hidden="!removable || undefined"
            @click="emit('remove')"
        >
            {{ removeLabel }}
        </button>
    </fieldset>
</template>
