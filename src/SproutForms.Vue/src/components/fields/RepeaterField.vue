<script setup lang="ts">
import { getEntries, type RepeaterFieldConfiguration } from '@sproutforms/client';
import { computed, nextTick, ref, watch } from 'vue';
import { useSproutFormContext } from '../../context';
import { fieldControlProps } from '../../fieldProps';

const props = defineProps(fieldControlProps);

const form = useSproutFormContext();
const RepeaterEntry = form.resolveComponent('RepeaterEntry');

const config = computed(() => (props.field.configuration ?? {}) as Partial<RepeaterFieldConfiguration>);
const entries = computed(() => getEntries(props.modelValue));
const canAdd = computed(() => config.value.maxItems == null || entries.value.length < config.value.maxItems);
const canRemove = computed(() => config.value.minItems == null || entries.value.length > config.value.minItems);

// Entries have no id of their own, so each gets a key here that stays with it when an entry before it is removed
let nextKey = 0;
const keys = ref<number[]>(entries.value.map(() => nextKey++));
watch(() => entries.value.length, length => {
    while (keys.value.length < length) keys.value.push(nextKey++);
    keys.value.length = length;
});

const entriesElement = ref<HTMLElement>();

async function add(): Promise<void> {
    form.engine.addEntry(props.path);
    await nextTick();
    entriesElement.value?.lastElementChild?.querySelector<HTMLElement>('input:not([type=hidden]), select, textarea')?.focus();
}

function remove(index: number): void {
    keys.value.splice(index, 1);
    form.engine.removeEntry(props.path, index);
}
</script>

<!-- Renders its own label, as the legend of its entries -->
<template>
    <fieldset :id="id" class="form-repeater" tabindex="-1" :data-sf-repeater="path" :aria-describedby="describedBy">
        <legend class="form-repeater-title">
            {{ field.label }}
            <span v-if="required" class="required">*</span>
        </legend>
        <div ref="entriesElement" class="form-repeater-entries" data-sf-repeater-entries>
            <component
                :is="RepeaterEntry"
                v-for="(_, index) in entries"
                :key="keys[index]"
                :field="field"
                :path="path"
                :index="index"
                :removable="canRemove && !disabled"
                @remove="remove(index)"
            />
        </div>
        <button type="button" class="form-repeater-add" data-sf-repeater-add :hidden="!canAdd || undefined" :disabled="disabled" @click="add">
            {{ config.addLabel }}
        </button>
    </fieldset>
</template>
