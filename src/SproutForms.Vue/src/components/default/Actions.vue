<script setup lang="ts">
import { getVisiblePageIndexes } from '@sproutforms/client';
import { computed } from 'vue';
import { useSproutFormContext } from '../../context';

const form = useSproutFormContext();

const isPaged = form.definition.pages.length > 1;
const page = computed(() => form.definition.pages.find(it => it.index === form.state.value.pageIndex) ?? form.definition.pages[0]);
const visiblePages = computed(() => getVisiblePageIndexes(form.definition, form.state.value.values, form.state.value.pageIndex));
const isFirst = computed(() => visiblePages.value.indexOf(form.state.value.pageIndex) <= 0);
const isLast = computed(() => visiblePages.value.indexOf(form.state.value.pageIndex) === visiblePages.value.length - 1);
const busy = computed(() => form.state.value.status === 'submitting' || form.state.value.status === 'validating');
</script>

<!-- Only the buttons the page needs show: previous after the first page, next before the last, submit on the last -->
<template>
    <div class="form-actions">
        <template v-if="isPaged">
            <button type="button" class="form-previous" data-sf-previous :hidden="isFirst || undefined" :disabled="busy" @click="form.previous()">
                {{ page.previousLabel }}
            </button>
            <button type="button" class="form-next" data-sf-next :hidden="isLast || undefined" :disabled="busy" @click="form.next()">
                {{ page.nextLabel }}
            </button>
        </template>
        <button type="submit" :hidden="(isPaged && !isLast) || undefined" :disabled="busy">{{ form.definition.submitLabel }}</button>
    </div>
</template>
