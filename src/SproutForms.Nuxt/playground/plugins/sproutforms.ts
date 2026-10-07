import CountedTextarea from '../customizations/CountedTextarea.vue';

// Runs after the module's plugin, so $sproutForms is there: this replaces the textarea of every form in the app
export default defineNuxtPlugin(nuxtApp => {
    nuxtApp.$sproutForms.registerField('textarea', CountedTextarea);
});
