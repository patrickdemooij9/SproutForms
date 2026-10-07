# @sproutforms/nuxt

Nuxt module for [SproutForms](https://github.com/patrickdemooij9/SproutForms) headless forms. It renders forms on the server with [`@sproutforms/vue`](../SproutForms.Vue), hands their definitions to the browser in the payload, and sends the browser's requests through your Nuxt server, so the API key never reaches the browser and Umbraco needs no CORS for it.

## Getting started

```ts
// nuxt.config.ts
export default defineNuxtConfig({
    modules: ['@sproutforms/nuxt'],
    sproutForms: {
        baseUrl: 'https://cms.example.com'
    }
});
```

```vue
<SproutForm alias="contact" />
```

`<SproutForm>`, `useSproutForm`, `useSproutForms`, `defineTheme` and `fieldControlProps` are auto-imported. See the [`@sproutforms/vue` README](../SproutForms.Vue/README.md) for the component and for replacing parts of the form.

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
});
```

A plugin runs per request on the server, as does the module's, so registrations don't leak between requests.

## Development

From the repository root, `npm install` once and build the packages the module uses (`npm run build -w @sproutforms/client && npm run build -w @sproutforms/vue`). Then, with the demo site's `AiTest` profile running (`pwsh -File scripts/ai-test/run-site.ps1`):

- `npm run dev:nuxt` starts the playground on `http://localhost:3000`. It renders the `aiTestHeadlessBasics` form on the server (`?form=` for another one), with a field type override in `playground/plugins/sproutforms.ts` and an alias override.
- `npm run preview:nuxt` builds the playground and serves the production build.
- `npm run build -w @sproutforms/nuxt` builds the module.

The playground runs on Nuxt 4.5.2: with Nuxt 4.6.0, a plain app in this workspace fails every server render with "Either manifest or precomputed data must be provided", also without this module.
