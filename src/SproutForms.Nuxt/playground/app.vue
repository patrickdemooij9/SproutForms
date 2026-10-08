<script setup lang="ts">
import UppercaseText from './customizations/UppercaseText.vue';

const route = useRoute();
const alias = computed(() => typeof route.query.form === 'string' ? route.query.form : 'aiTestHeadlessBasics');
</script>

<template>
    <main>
        <h1>SproutForms Nuxt playground</h1>
        <p>Rendered on the server. Every textarea counts its characters (a field type override in plugins/sproutforms.ts), and this form's company is in capitals (an alias override).</p>
        <SproutForm :alias="alias" :fields="{ company: UppercaseText }" />
    </main>
</template>

<style>
body {
    margin: 0;
    font-family: system-ui, sans-serif;
    background: #fff;
    color: #222;
}

main {
    max-width: 48rem;
    margin: 0 auto;
    padding: 1rem;
}
</style>
