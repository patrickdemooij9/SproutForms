import {
    createSproutFormsClient,
    globalOutcomeHandlers,
    globalSubmissionGuards,
    globalValidators,
    Registry,
    type FormClientModel,
    type OutcomeContext,
    type OutcomeHandler,
    type SproutFormsClient,
    type SproutFormsClientOptions,
    type SubmissionGuardHandler,
    type Validator
} from '@sproutforms/client';
import { inject, onMounted, onServerPrefetch, ref, watch, type App, type Component, type InjectionKey, type Ref } from 'vue';
import { builtInComponents, builtInFields } from './components/builtIn';
import { defaultThemeName, type FormComponentName, type SproutFormsTheme } from './themes';

/**
 * What an outcome handler can do in Vue on top of the client's context.
 */
export interface VueOutcomeContext extends OutcomeContext {
    // Shows the form's Success component with this HTML instead of the form
    showMessage(html: string): void;
    // Goes to a URL: in the router when it is on this site, otherwise by loading it
    navigate(url: string): void | Promise<void>;
}

export type VueOutcomeHandler = OutcomeHandler<VueOutcomeContext>;

/**
 * A form definition being loaded.
 */
export interface DefinitionRequest {
    definition: Readonly<Ref<FormClientModel | undefined>>;
    error: Readonly<Ref<unknown>>;
}

/**
 * Loads a form definition by id or alias inside a component's setup, also during server-side rendering.
 */
export type DefinitionLoader = (idOrAlias: () => string) => DefinitionRequest;

export interface SproutFormsOptions {
    // A client, or the options to create one with. A form that gets its definition passed in only needs one to submit
    client?: SproutFormsClient | SproutFormsClientOptions;
    // Keyed by theme name. The "default" theme replaces built-in components for every form
    themes?: Record<string, SproutFormsTheme>;
    // Keyed by rule type, over the global ones
    validators?: Record<string, Validator>;
    // Keyed by submission guard alias, over the global ones
    guards?: Record<string, SubmissionGuardHandler>;
    // Keyed by outcome type, over the global ones and the built-in message and redirect handlers
    outcomeHandlers?: Record<string, VueOutcomeHandler>;
    // How a redirect outcome goes to a URL; by default in vue-router when it is installed and the URL is on this site
    navigate?: (url: string) => void | Promise<void>;
    // How <SproutForm alias="…"> loads its definition; Nuxt replaces it with useAsyncData
    loadDefinition?: DefinitionLoader;
}

// The URL as a path in this site when it is on this site, so the router can take it
function toInternalPath(url: string): string | undefined {
    if (url.startsWith('/') && !url.startsWith('//')) return url;
    const location = (globalThis as { location?: Location }).location;
    if (!location) return undefined;
    try {
        const target = new URL(url, location.href);
        return target.origin === location.origin ? target.pathname + target.search + target.hash : undefined;
    } catch {
        return undefined;
    }
}

const builtInOutcomeHandlers: Record<string, VueOutcomeHandler> = {
    message: (outcome, context) => {
        const message = outcome.data.message;
        context.showMessage(typeof message === 'string' && message.trim() ? message : '');
    },
    redirect: (outcome, context) => {
        if (typeof outcome.data.url === 'string') return context.navigate(outcome.data.url);
    },
    redirectUmbracoPage: (outcome, context) => {
        if (typeof outcome.data.url === 'string') return context.navigate(outcome.data.url);
    }
};

function toRegistry<T>(parent: { get(key: string): T | undefined }, items: Record<string, T> = {}): Registry<T> {
    const registry = new Registry<T>(parent);
    for (const [key, item] of Object.entries(items)) registry.register(key, item);
    return registry;
}

export const sproutFormsKey: InjectionKey<SproutForms> = Symbol('sproutforms');

/**
 * Loads the definition when the component renders on a server and, when it wasn't loaded there, once it is mounted.
 */
function createDefaultLoader(getClient: () => SproutFormsClient): DefinitionLoader {
    return idOrAlias => {
        const definition = ref<FormClientModel>();
        const error = ref<unknown>();

        async function load() {
            error.value = undefined;
            try {
                definition.value = await getClient().getDefinition(idOrAlias());
            } catch (caught) {
                error.value = caught;
            }
        }

        onServerPrefetch(load);
        onMounted(() => {
            if (!definition.value) void load();
        });
        watch(idOrAlias, load);
        return { definition, error };
    };
}

/**
 * Creates the plugin that gives every <SproutForm> in the app its client, themes and handlers. Create one per app, and on a
 * server one per request: what you register on it stays in that app.
 */
export function createSproutForms(options: SproutFormsOptions = {}) {
    const client = options.client && 'submit' in options.client ? options.client : options.client ? createSproutFormsClient(options.client) : undefined;
    const themes = new Map<string, SproutFormsTheme>();
    let installedApp: App | undefined;

    function getClient(): SproutFormsClient {
        if (!client) throw new Error('SproutForms needs a client: pass `client` to createSproutForms.');
        return client;
    }

    function getTheme(name: string): SproutFormsTheme {
        let theme = themes.get(name);
        if (!theme) {
            theme = { components: {}, fields: {} };
            themes.set(name, theme);
        }
        return theme;
    }

    const sproutForms = {
        client,
        validators: toRegistry(globalValidators, options.validators),
        guards: toRegistry(globalSubmissionGuards, options.guards),
        outcomeHandlers: toRegistry<VueOutcomeHandler>(
            { get: key => globalOutcomeHandlers.get(key) ?? builtInOutcomeHandlers[key] },
            options.outcomeHandlers
        ),
        loadDefinition: options.loadDefinition ?? createDefaultLoader(getClient),
        getClient,

        /**
         * Adds a theme's components, or replaces those it already had.
         */
        registerTheme(name: string, theme: SproutFormsTheme): void {
            const existing = getTheme(name);
            Object.assign(existing.components!, theme.components);
            Object.assign(existing.fields!, theme.fields);
        },

        /**
         * The control for a field type, such as a custom type's alias, in a theme; the default theme when none is named.
         */
        registerField(type: string, component: Component, theme = defaultThemeName): void {
            getTheme(theme).fields![type] = component;
        },

        registerComponent(name: FormComponentName, component: Component, theme = defaultThemeName): void {
            getTheme(theme).components![name] = component;
        },

        resolveField(type: string, theme = defaultThemeName): Component | undefined {
            return themes.get(theme)?.fields?.[type] ?? themes.get(defaultThemeName)?.fields?.[type] ?? builtInFields[type];
        },

        resolveComponent(name: FormComponentName, theme = defaultThemeName): Component {
            return themes.get(theme)?.components?.[name] ?? themes.get(defaultThemeName)?.components?.[name] ?? builtInComponents[name];
        },

        async navigate(url: string): Promise<void> {
            if (options.navigate) return options.navigate(url);

            // Looked up here rather than on install, so the router can be installed after this plugin
            const router = installedApp?.config.globalProperties.$router as { push(path: string): Promise<unknown> } | undefined;
            const path = toInternalPath(url);
            if (router && path) {
                await router.push(path);
            } else {
                (globalThis as { location?: Location }).location?.assign(url);
            }
        },

        install(app: App): void {
            installedApp = app;
            app.provide(sproutFormsKey, sproutForms);
        }
    };

    for (const [name, theme] of Object.entries(options.themes ?? {})) {
        sproutForms.registerTheme(name, theme);
    }
    return sproutForms;
}

export type SproutForms = ReturnType<typeof createSproutForms>;

/**
 * The plugin installed in the app.
 */
export function useSproutFormsPlugin(): SproutForms {
    const sproutForms = inject(sproutFormsKey, undefined);
    if (!sproutForms) throw new Error('SproutForms is not installed: call app.use(createSproutForms(...)).');
    return sproutForms;
}
