export default defineNuxtConfig({
    modules: ['../src/module'],
    // The demo site's AiTest profile; NUXT_SPROUT_FORMS_BASE_URL for another Umbraco site with SproutForms:Headless:Enabled
    sproutForms: {
        baseUrl: 'http://localhost:5970'
    },
    // One of SproutForms:Headless:AllowedOrigins in the demo site's appsettings.AiTest.json, so the page URL is stored
    devServer: {
        port: 3000
    },
    devtools: { enabled: false },
    compatibilityDate: '2026-10-01'
});
