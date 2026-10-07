import { createSproutFormsClient } from '@sproutforms/client';
import { createSproutForms, type SproutForms } from '@sproutforms/vue';
import { computed } from 'vue';
import type { Plugin } from '#app';
import { defineNuxtPlugin, navigateTo, useAsyncData, useRuntimeConfig } from '#imports';

// A URL on another site, which navigateTo only loads when told so
function isExternal(url: string): boolean {
    if (url.startsWith('/') && !url.startsWith('//')) return false;
    return !import.meta.client || new URL(url, window.location.href).origin !== window.location.origin;
}

/**
 * One SproutForms per app, so per request on the server. Register themes, fields and handlers on `nuxtApp.$sproutForms` in a
 * plugin of your own.
 */
const plugin: Plugin<{ sproutForms: SproutForms }> = defineNuxtPlugin(nuxtApp => {
    const config = useRuntimeConfig();

    // Only the server has the key: the browser goes through the proxy, or to Umbraco without one
    const client = import.meta.server
        ? createSproutFormsClient({
            baseUrl: config.sproutForms.baseUrl,
            apiPath: config.sproutForms.apiPath,
            apiKey: config.sproutForms.apiKey || undefined
        })
        : createSproutFormsClient({
            baseUrl: config.public.sproutForms.baseUrl,
            apiPath: config.public.sproutForms.apiPath
        });

    const sproutForms = createSproutForms({
        client,
        navigate: async url => {
            await navigateTo(url, { external: isExternal(url) });
        },
        // Loaded during server-side rendering and handed to the browser in the payload, so it doesn't load again
        loadDefinition: idOrAlias => {
            const { data, error } = useAsyncData(
                () => `sproutforms:${idOrAlias()}`,
                () => client.getDefinition(idOrAlias())
            );
            return {
                definition: computed(() => data.value ?? undefined),
                error: computed(() => error.value ?? undefined)
            };
        }
    });

    nuxtApp.vueApp.use(sproutForms);
    return { provide: { sproutForms } };
});

export default plugin;
