<script setup lang="ts">
import { getVisiblePageIndexes } from '@sproutforms/client';
import { computed, nextTick, ref, watch } from 'vue';
import { useSproutFormContext } from '../../context';
import { defaultThemeName } from '../../themes';

const form = useSproutFormContext();
const Progress = form.resolveComponent('Progress');
const Rows = form.resolveComponent('Rows');
const Errors = form.resolveComponent('Errors');
const SubmissionGuard = form.resolveComponent('SubmissionGuard');
const Actions = form.resolveComponent('Actions');
const Success = form.resolveComponent('Success');

// A form with one page renders without a page wrapper, progress or navigation, as in Razor
const isPaged = form.definition.pages.length > 1;
const visiblePages = computed(() => getVisiblePageIndexes(form.definition, form.state.value.values, form.state.value.pageIndex));
const themeAttribute = form.theme === defaultThemeName ? undefined : form.theme;

// Every page is rendered and only the current one shows, as in Razor, so what is entered in an upload stays when going back
const pageElements = ref<Record<number, HTMLElement>>({});

watch(() => form.state.value.pageIndex, async index => {
    // A page with errors leaves the focus to its first error
    if (Object.keys(form.state.value.errors).length > 0) return;
    await nextTick();
    pageElements.value[index]?.focus();
});
</script>

<template>
    <div class="form-root" :data-sf-theme="themeAttribute">
        <component :is="Success" v-if="form.success.value" v-bind="form.success.value" />
        <form
            v-else
            novalidate
            :data-sf-paged="isPaged || undefined"
            :aria-busy="form.state.value.status === 'submitting' || form.state.value.status === 'validating' || undefined"
            @submit.prevent="form.submit()"
        >
            <component :is="SubmissionGuard" />
            <component :is="Progress" v-if="isPaged && form.definition.showProgress" />
            <template v-if="isPaged">
                <fieldset
                    v-for="page in form.definition.pages"
                    :key="page.index"
                    :ref="element => { if (element) pageElements[page.index] = element as HTMLElement; }"
                    class="form-page"
                    tabindex="-1"
                    :data-sf-page="page.index"
                    :data-sf-skipped="!visiblePages.includes(page.index) || undefined"
                    :hidden="page.index !== form.state.value.pageIndex || undefined"
                >
                    <legend v-if="page.title" class="form-page-title">{{ page.title }}</legend>
                    <component :is="Rows" :rows="page.rows" />
                </fieldset>
            </template>
            <template v-else>
                <component :is="Rows" v-for="page in form.definition.pages" :key="page.index" :rows="page.rows" />
            </template>
            <component :is="Errors" />
            <component :is="Actions" />
        </form>
    </div>
</template>
