<script setup lang="ts">
import { getVisiblePageIndexes } from '@sproutforms/client';
import { computed } from 'vue';
import { useSproutFormContext } from '../../context';

const form = useSproutFormContext();

// The pages the visitor goes through: skipped pages leave the progress, and those before the current one are complete
const visiblePages = computed(() => getVisiblePageIndexes(form.definition, form.state.value.values, form.state.value.pageIndex));
const currentPosition = computed(() => visiblePages.value.indexOf(form.state.value.pageIndex));
</script>

<template>
    <ol class="form-progress" data-sf-progress>
        <li
            v-for="page in form.definition.pages"
            :key="page.index"
            class="form-progress-step"
            :data-sf-progress-step="page.index"
            :hidden="!visiblePages.includes(page.index) || undefined"
            :data-sf-complete="visiblePages.indexOf(page.index) !== -1 && visiblePages.indexOf(page.index) < currentPosition || undefined"
            :aria-current="page.index === form.state.value.pageIndex ? 'step' : undefined"
        >
            {{ page.progressLabel }}
        </li>
    </ol>
</template>
