import { createSproutForms, defineTheme } from '@sproutforms/vue';
import { createApp } from 'vue';
import '../../../SproutForms.Core/wwwroot/forms-src/forms-layout.css';
import '../../../SproutForms.Core/wwwroot/forms-src/forms-default-theme.css';
import App from './App.vue';
import InlineField from './customizations/InlineField.vue';
import './style.css';

const sproutForms = createSproutForms({
    // The demo site's AiTest profile; set VITE_SPROUTFORMS_URL for another Umbraco site with SproutForms:Headless:Enabled
    client: { baseUrl: import.meta.env.VITE_SPROUTFORMS_URL ?? 'http://localhost:5970' },
    themes: {
        // A theme that only replaces the field wrapper; everything else comes from the default theme
        inline: defineTheme({ components: { Field: InlineField } })
    }
});

createApp(App).use(sproutForms).mount('#app');
