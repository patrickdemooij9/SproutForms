# Nuxt: `@sproutforms/nuxt`

`@sproutforms/nuxt` is the Nuxt module for SproutForms headless forms. It renders forms on the server with [`@sproutforms/vue`](vue.md), hands their definitions to the browser in the payload, and sends the browser's requests through your Nuxt server, so the API key never reaches the browser and Umbraco needs no CORS for it.

## Installation

Turn the headless API on in the Umbraco site first; see [Install headless](README.md#turning-it-on).

```
npm install @sproutforms/nuxt
```

> `@sproutforms/nuxt` and the packages it uses aren't published to npm yet. Until they are, build them from the repository (see [Contributing](../../contributing.md)).

```ts
// nuxt.config.ts
export default defineNuxtConfig({
    modules: ['@sproutforms/nuxt'],
    sproutForms: {
        baseUrl: 'https://cms.example.com'
    }
});
```

The module needs Nuxt 3.16 or later.

## Rendering a form

```vue
<SproutForm alias="contact" />
```

`<SproutForm>`, `useField`, `useSproutFormContext`, `useSproutFormsPlugin`, `defineTheme` and `fieldControlProps` are auto-imported. `<SproutForm>` takes the same props and emits the same events as in [Vue](vue.md#rendering-a-form). The definition is loaded with `useAsyncData` during server rendering, so the browser doesn't load it again.

## Options

| Option | Default | |
|---|---|---|
| `baseUrl` | | The Umbraco site. `NUXT_SPROUT_FORMS_BASE_URL` at runtime. |
| `apiKey` | | Only when `SproutForms:Headless:ApiKey` is set. Stays on the server. `NUXT_SPROUT_FORMS_API_KEY` at runtime. |
| `apiPath` | `/umbraco/sproutforms/delivery/api/v1` | Where the headless API is under `baseUrl`. |
| `proxy` | `true` | Sends the browser's requests through this site, which adds the API key. Without it the browser calls `baseUrl` itself, without a key, so the site's origin must be in `SproutForms:Headless:AllowedOrigins`. |
| `proxyPath` | `/api/_sproutforms` | Where the proxy listens. It only forwards the headless API's endpoints. |
| `css` | `true` | Adds the layout and default theme stylesheets. |

Add your site's origin to `SproutForms:Headless:AllowedOrigins` either way: Umbraco only stores a submission's page URL when it is on an allowed origin.

## Replacing parts of the form

Register themes, field types and handlers on `$sproutForms` in a plugin of your own. Module plugins run first, so it is there:

```ts
// plugins/sproutforms.ts
import StarRating from '~/components/StarRating.vue';
import CompactField from '~/components/CompactField.vue';

export default defineNuxtPlugin(nuxtApp => {
    nuxtApp.$sproutForms.registerField('starRating', StarRating);
    nuxtApp.$sproutForms.registerTheme('compact', { components: { Field: CompactField } });

    // Validators, submission guards and outcome handlers go in the plugin's registries
    nuxtApp.$sproutForms.validators.register('postcode', value => /^\d{4} ?[A-Z]{2}$/i.test(value));
    nuxtApp.$sproutForms.outcomeHandlers.register('quizResult', (outcome, { showMessage }) =>
        showMessage(`<p>You scored ${Number(outcome.data.score)}</p>`));
});
```

A plugin runs per request on the server, as does the module's, so registrations don't leak between requests. See [Vue](vue.md#replacing-parts-of-the-form) for the components you can replace, and [Extending for headless front-ends](../../extending-headless/README.md) for validators, guards and outcome handlers.

## Redirects

The module's `navigate` uses Nuxt's `navigateTo`, so a `redirect` or `redirectUmbracoPage` outcome on your own site stays in the app, and one on another site loads it.

## Playground

`npm run dev:nuxt` in the repository root starts the Nuxt playground; see [Contributing](../../contributing.md#vue-and-nuxt).
