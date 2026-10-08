# @sproutforms/client

A framework-agnostic client for the [SproutForms](https://github.com/patrickdemooij9/SproutForms) headless API. It fetches a published form, evaluates its conditions and validation rules the way the server does, and submits it. It doesn't render anything: [`@sproutforms/vue`](../SproutForms.Vue) and [`@sproutforms/nuxt`](../SproutForms.Nuxt) render forms with it, and you can build a renderer for any other framework on its form engine.

Turn the headless API on in the Umbraco site first. The [README's Headless section](https://github.com/patrickdemooij9/SproutForms#headless) covers the settings, the endpoints and the client.

```ts
import { createSproutFormsClient, validateForm } from '@sproutforms/client';

const client = createSproutFormsClient({ baseUrl: 'https://cms.example.com' });
const form = await client.getDefinition('contact');
const errors = await validateForm(form, values);
const result = await client.submit(form, { values });
```

## The form engine

`createFormEngine` holds one form's state and everything that changes it, without touching the DOM, so it runs on a server too. A renderer subscribes to it and renders its state:

```ts
import { createFormEngine } from '@sproutforms/client';

const engine = createFormEngine(form, { client });
engine.subscribe(state => render(state));   // state: values, variables, errors, status, outcome, submitError

engine.setValue('email', 'ann@example.com'); // works the variables out again and drops the field's error
engine.setValue('people[0].email', 'bob@example.com'); // a field in a repeater's entry, by its path
engine.addEntry('people');                    // and removeEntry('people', 0)

await engine.next();                          // validates the page in the browser and with the server, then goes on
const result = await engine.submit();        // validates the whole form, then submits; errors end up in state.errors
```

On a form with more than one page, `state.pageIndex` is the page the visitor is on, `getVisiblePageIndexes()` the pages they go through and `isLastPage()` whether to show the submit button. A submit with errors goes back to the first page that has one.

Create one engine per form on the page, and on a server one per request.

## Registries

`registerValidator`, `registerSubmissionGuard` and `registerOutcomeHandler` fill global registries. To keep what one app registers out of another, such as per request on a server, create a `Registry` with the global one as its parent and pass it on: `validateForm(form, values, { validators })`, `createFormEngine(form, { validators, guards })`, `client.submit(form, { values, guards })` and `handleOutcome(outcome, context, handlers)`.

## Other hosts

`createSproutFormsClient({ baseUrl, apiPath })` takes the API's path when it isn't Umbraco's `/umbraco/sproutforms/delivery/api/v1`, such as a proxy on your own server. An empty `baseUrl` calls the site the page is on.

## Development

The packages are an npm workspace: run `npm install` in the repository root.

- `npm run build` compiles to `dist`.
- `npm run generate` regenerates `src/api/types.gen.ts` from the OpenAPI document. Run it while the demo site's `AiTest` profile is running.
- `example/` is a playground that renders any form of the demo site and shows every request and the stored submission. `pwsh -File scripts/ai-test/run-headless-playground.ps1` from the repository root starts it with the demo site.

The condition and validation code is also bundled into the package's `forms.js` by `npm run minify:forms` in `src/SproutForms.Umbraco/assets`. Run that after changing `conditions.ts` or `validation.ts`.
