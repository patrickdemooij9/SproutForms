import { addComponent, addImports, addPlugin, addServerHandler, createResolver, defineNuxtModule } from '@nuxt/kit';
import { defu } from 'defu';

const defaultApiPath = '/umbraco/sproutforms/delivery/api/v1';

export interface ModuleOptions {
    // The Umbraco site, such as https://cms.example.com; NUXT_SPROUT_FORMS_BASE_URL at runtime
    baseUrl: string;
    // Only when SproutForms:Headless:ApiKey is set. It stays on the server; NUXT_SPROUT_FORMS_API_KEY at runtime
    apiKey: string;
    // Where the API is under baseUrl
    apiPath: string;
    // Sends the browser's requests through this site, which adds the API key, so the browser needs neither the key nor CORS.
    // Without it the browser calls baseUrl itself, without a key
    proxy: boolean;
    // Where the proxy listens
    proxyPath: string;
    // Adds @sproutforms/vue's layout and theme stylesheets
    css: boolean;
}

export default defineNuxtModule<ModuleOptions>({
    meta: {
        name: '@sproutforms/nuxt',
        configKey: 'sproutForms',
        compatibility: {
            nuxt: '>=3.16.0'
        }
    },
    defaults: {
        baseUrl: '',
        apiKey: '',
        apiPath: defaultApiPath,
        proxy: true,
        proxyPath: '/api/_sproutforms',
        css: true
    },
    setup(options, nuxt) {
        const resolver = createResolver(import.meta.url);

        // The server talks to Umbraco itself; the browser through the proxy, or straight to Umbraco without a key
        nuxt.options.runtimeConfig.sproutForms = defu(nuxt.options.runtimeConfig.sproutForms as Partial<ModuleOptions> | undefined, {
            baseUrl: options.baseUrl,
            apiKey: options.apiKey,
            apiPath: options.apiPath,
            proxyPath: options.proxyPath
        });
        nuxt.options.runtimeConfig.public.sproutForms = defu(nuxt.options.runtimeConfig.public.sproutForms as Record<string, unknown> | undefined, {
            baseUrl: options.proxy ? '' : options.baseUrl,
            apiPath: options.proxy ? options.proxyPath : options.apiPath
        });

        addPlugin(resolver.resolve('./runtime/plugin'));
        addComponent({ name: 'SproutForm', export: 'SproutForm', filePath: '@sproutforms/vue' });
        addImports([
            { name: 'useSproutFormContext', from: '@sproutforms/vue' },
            { name: 'useSproutFormsPlugin', from: '@sproutforms/vue' },
            { name: 'useField', from: '@sproutforms/vue' },
            { name: 'defineTheme', from: '@sproutforms/vue' },
            { name: 'fieldControlProps', from: '@sproutforms/vue' }
        ]);

        if (options.proxy) {
            addServerHandler({ route: `${options.proxyPath}/**`, handler: resolver.resolve('./runtime/server/proxy') });
        }

        if (options.css) {
            nuxt.options.css.push('@sproutforms/vue/layout.css', '@sproutforms/vue/theme.css');
        }
    }
});
