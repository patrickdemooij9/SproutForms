<script setup lang="ts">
import { isPageVisible } from '@sproutforms/client';
import { computed } from 'vue';
import { useSproutForm } from '../../context';
import { defaultThemeName } from '../../themes';

const form = useSproutForm();
const Rows = form.resolveComponent('Rows');
const Errors = form.resolveComponent('Errors');
const Actions = form.resolveComponent('Actions');
const Success = form.resolveComponent('Success');

// A form with one page renders without a page wrapper, as in Razor
const isPaged = form.definition.pages.length > 1;
const pages = computed(() => form.definition.pages.filter(page => isPageVisible(page, form.state.value.values, form.state.value.variables)));
const themeAttribute = form.theme === defaultThemeName ? undefined : form.theme;
</script>

<template>
    <div class="form-root" :data-sf-theme="themeAttribute">
        <component :is="Success" v-if="form.success.value" v-bind="form.success.value" />
        <form
            v-else
            novalidate
            :data-sf-paged="isPaged || undefined"
            :aria-busy="form.state.value.status === 'submitting' || undefined"
            @submit.prevent="form.submit()"
        >
            <template v-for="page in pages" :key="page.index">
                <fieldset v-if="isPaged" class="form-page" :data-sf-page="page.index">
                    <legend v-if="page.title" class="form-page-title">{{ page.title }}</legend>
                    <component :is="Rows" :rows="page.rows" />
                </fieldset>
                <component :is="Rows" v-else :rows="page.rows" />
            </template>
            <component :is="Errors" />
            <component :is="Actions" />
        </form>
    </div>
</template>
