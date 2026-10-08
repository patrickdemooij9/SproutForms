# Install headless

A front-end that isn't rendered by Umbraco, such as a Next.js, Nuxt or mobile app, can use SproutForms through its headless API. The API returns a published form's structure as JSON and accepts submissions. The front-end renders the form itself, with the JavaScript packages or with code of its own.

> The headless API is new, and it may still change until SproutForms 1.0. Its routes are versioned (`/v1/`).

The headless API is part of the Umbraco package. Install SproutForms in Umbraco first, see [Install with Umbraco](../umbraco.md). Editors build the forms in the backoffice (or you define them in code), and the front-end renders them.

Then choose how the front-end renders forms:

- [Vue](vue.md) with `@sproutforms/vue`: renders a form with the same markup and stylesheets as the Razor forms, without any code of your own.
- [Nuxt](nuxt.md) with `@sproutforms/nuxt`: the same, rendered on the server, with a proxy that keeps the API key on your server.
- Any other framework with [`@sproutforms/client`](#the-javascript-client), which does everything but the rendering.
- Plain HTTP calls to the [endpoints](#endpoints).

> The npm packages `@sproutforms/client`, `@sproutforms/vue` and `@sproutforms/nuxt` aren't published to npm yet. Until they are, build them from the repository (see [Contributing](../../contributing.md)).

## Turning it on

The headless API is off by default. Turn it on in the Umbraco site's `appsettings.json`:

```json
"SproutForms": {
  "Headless": {
    "Enabled": true,
    "AllowedOrigins": [ "https://www.example.com" ],
    "ApiKey": ""
  }
}
```

- `Enabled`: turns on the endpoints. While it's off, they answer 404. Changing it needs a restart for CORS and the OpenAPI document.
- `AllowedOrigins`: the front-ends that call the API from a browser. They're allowed by CORS. A submission's page URL (`sf_PageUrl`) is only stored when it's on this site or on one of these origins; otherwise it's dropped.
- `ApiKey`: when set, every request must send it in the `Api-Key` header. Only use it when your front-end calls the API from its own server. A key in browser code is public.

The API has no antiforgery token, since the front-end runs on another origin. The submission guard keeps bots out instead. That's a honeypot field by default, or reCAPTCHA v3 (see [Configuration](../../configuration.md#recaptcha-v3)). A bot can post to the API directly and leave the honeypot empty, so use reCAPTCHA for a form that attracts spam.

## Endpoints

All routes start with `/umbraco/sproutforms/delivery/api/v1`. The OpenAPI document is at `/umbraco/swagger/sproutforms-delivery/swagger.json`.

| Endpoint | Does |
|---|---|
| `GET definitions/{idOrAlias}` | The published form: its pages, rows and columns, each column holding its field with its configuration, rules and validation rules (the same shape as the Razor view model), the submission guard's settings and the default texts. The `ETag` is the published version, so a client can revalidate with `If-None-Match` (304). |
| `POST entries/{id}` | Submits the form. The body is its values as a JSON object, the same values a Razor form posts: `{ "alias": value, "sf_PageUrl": "...", ... }`, with what the submission guard checks next to the fields. A repeater's value is a list of entry objects, `[{ "alias": value }]`. With uploads, send `multipart/form-data` instead: the values as a JSON part named `values`, and each file as a part named by its field's path, such as `cv` or `people[0].cv`. Returns `{ "outcome": { "type", "data" }, "variables": { ... } }`, with the values of the variables the definition holds (those its conditions use). |
| `POST entries/{id}/pages/{index}/validate` | Checks one page of a paged form with the values entered so far, as the same JSON object, without saving anything. Returns 204 when the page is valid. |

`entries/{id}` takes the form's id, not its alias; the definition holds it in `id`. A form in the recycle bin answers 404, as if it didn't exist.

A rejected submission or page returns 400 as `application/problem+json`, with the errors keyed by field alias in `errors`. `submissionGuard`, and any other key that isn't a field, is about the whole form.

The values also hold what the submission guard checks: `"g-recaptcha-response"` with the token for reCAPTCHA, or the honeypot's field (its name is in `submissionGuard.settings.fieldName`) with whatever that hidden input holds. The client adds them, and the page URL, for you.

The outcome's `data` depends on its type:
- `message` has `message`.
- `redirect` has `url`.
- `redirectUmbracoPage` has `url`, `path` and `contentKey`, for your own router.
- An outcome type of your own has whatever it returns, see [Custom outcome handlers](../../extending-headless/outcome-handlers.md).

When `outcome` is null, the submission was still saved. Show the form's `texts.submitSucceeded`.

The definition never holds what a visitor mustn't see: the storage provider of an upload, workflows, the outcome's settings, the calculations of variables no condition uses, or a form type's settings and field extensions (such as a quiz's correct answers). See [Showing settings to the browser](../../extending-headless/client-settings.md).

## The JavaScript client

[`@sproutforms/client`](../../../src/SproutForms.Client) is a framework-agnostic client for the API, written in TypeScript. It doesn't render anything; it gives your components what they need.

```
npm install @sproutforms/client
```

```ts
import { createSproutFormsClient, validateForm, isFieldVisible, getNextPageIndex, handleOutcome, registerOutcomeHandler } from '@sproutforms/client';

const client = createSproutFormsClient({ baseUrl: 'https://cms.example.com' });
const form = await client.getDefinition('contact');

// Render form.pages[i].rows[j].columns[k].field, the same shape as the Razor view model.
// Hide a field while isFieldVisible(field, values) is false, and skip pages with getNextPageIndex(form, values, current).

const errors = await validateForm(form, values);            // the same rules and conditions as the server
if (Object.keys(errors).length === 0) {
    const result = await client.submit(form, { values });   // adds the guard's values; uses multipart when a value is a File
    if (!result.ok) showErrors(result.errors);               // result.variables: what the server worked out
    else if (!await handleOutcome(result.outcome, { definition: form })) showMessage(form.texts.submitSucceeded);
}

registerOutcomeHandler('redirectUmbracoPage', outcome => router.push(String(outcome.data.path)));
```

`createSproutFormsClient` takes:

| Option | |
|---|---|
| `baseUrl` | The Umbraco site, such as `https://cms.example.com`. Empty for the site the page is on, such as a proxy on your own server. |
| `apiPath` | Where the API is under `baseUrl`; `/umbraco/sproutforms/delivery/api/v1` by default. |
| `apiKey` | Only when `SproutForms:Headless:ApiKey` is set, and only from a server. |
| `fetch` | Your own `fetch`. |
| `headers` | Headers to send with every request. |

A request that fails other than on validation (404, 401, a server error) throws a `SproutFormsApiError` with its `status`.

The client also has:
- `client.validatePage` for paged forms, and `client.revalidateDefinition` to check whether a copy you cached is out of date (it returns `null` when it isn't).
- `calculateVariables(form, values)`, which works out the variables the definition holds the way the server does. Pass them to `isFieldVisible` and `isFieldRequired` when a form's conditions use variables; the page functions and `validateForm` work them out themselves.
- `isFieldOfType(field, 'select')`, which types a built-in field's `configuration`.
- `registerValidator` for the validation rules of a custom field type. Rules without a validator are only checked by the server.
- `registerSubmissionGuard` for a custom guard. The honeypot and reCAPTCHA v3 are built in; call `loadSubmissionGuard(form)` when the form shows.
- `registerOutcomeHandler` for what the visitor sees after a submit. No handlers are registered by default: what an outcome looks like is up to your front-end.

forms.js, which the Razor forms use, runs the same engine, so a Razor form and a headless form behave the same way.

### The form engine

`createFormEngine` holds one form's state and everything that changes it, without touching the DOM, so it runs on a server too. A renderer of your own subscribes to it and renders its state:

```ts
import { createFormEngine } from '@sproutforms/client';

const engine = createFormEngine(form, { transport: client });
engine.subscribe(state => render(state));   // state: values, variables, errors, status, outcome, submitError

engine.setValue('email', 'ann@example.com'); // works the variables out again and drops the field's error
engine.setValue('people[0].email', 'bob@example.com'); // a field in a repeater's entry, by its path
engine.addEntry('people');                    // and removeEntry('people', 0)

await engine.next();                          // validates the page in the browser and with the server, then goes on
const result = await engine.submit();        // validates the whole form, then submits; errors end up in state.errors
```

On a form with more than one page, `state.pageIndex` is the page the visitor is on, `getVisiblePageIndexes()` the pages they go through and `isLastPage()` whether to show the submit button. A submit with errors goes back to the first page that has one.

Create one engine per form on the page, and on a server one per request. The transport is how it reaches the server: a client from `createSproutFormsClient`, or any `FormTransport` (`validatePage` and `submit`).

### Registries

`registerValidator`, `registerSubmissionGuard` and `registerOutcomeHandler` fill global registries. To keep what one app registers out of another, such as per request on a server, create a `Registry` with the global one as its parent and pass it on: `validateForm(form, values, { validators })`, `createFormEngine(form, { validators, guards })`, `client.submit(form, { values, guards })` and `handleOutcome(outcome, context, handlers)`.

```ts
import { Registry, globalValidators, type Validator } from '@sproutforms/client';

const validators = new Registry<Validator>(globalValidators);
validators.register('postcode', value => /^\d{4} ?[A-Z]{2}$/i.test(value));
```

## Next steps

- [Vue](vue.md) and [Nuxt](nuxt.md).
- [Extending for headless front-ends](../../extending-headless/README.md): validators, guards, outcome handlers and controls for your own field types.
- The headless playground, which renders any form of the demo site and shows every request: see [Contributing](../../contributing.md#the-headless-playground).
