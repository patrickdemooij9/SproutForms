# Extending for headless front-ends

The server side of an extension is the same whether a form renders with Razor or in a headless front-end: see [Extending SproutForms](../extending/README.md). A headless front-end then needs the browser side of some of them, registered with `@sproutforms/client`, `@sproutforms/vue` or `@sproutforms/nuxt`.

| Extension | What the front-end needs |
|---|---|
| [Field type](field-types.md) | A control that renders it. |
| [Workflow type](workflow-types.md) | Nothing: workflows only run on the server. |
| [Validator](validators.md) | A validator for each rule type, to check it in the browser. Optional: the server checks every rule. |
| [Submission guard](submission-guards.md) | A handler that returns the values the guard checks. |
| [Outcome type](outcome-handlers.md) | A handler that shows the outcome. |
| [Settings](client-settings.md) | Nothing, but decide on the server what the browser may see. |

The handlers are the same in every package, and the same as forms.js takes for the Razor forms (`window.SproutForms.register...`), so one validator or guard handler works for both.

## Where to register

`@sproutforms/client` has global registries, filled with `registerValidator`, `registerSubmissionGuard` and `registerOutcomeHandler`:

```ts
import { registerValidator, registerSubmissionGuard, registerOutcomeHandler } from '@sproutforms/client';

registerValidator('postcode', value => /^\d{4} ?[A-Z]{2}$/i.test(value));
```

Everything uses the global registries unless it gets its own. On a server, where one process renders for many visitors, give each app or request a child `Registry` instead, so what one registers doesn't leak into the others; see [Registries](../getting-started/headless/README.md#registries).

`@sproutforms/vue` does that for you: `createSproutForms` takes `validators`, `guards` and `outcomeHandlers`, which only apply to that app and fall back to the global ones:

```ts
createSproutForms({
    client: { baseUrl: 'https://cms.example.com' },
    validators: { postcode: value => /^\d{4} ?[A-Z]{2}$/i.test(value) },
    outcomeHandlers: { quizResult: (outcome, { showMessage }) => showMessage(`<p>You scored ${Number(outcome.data.score)}</p>`) }
});
```

The plugin's `validators`, `guards` and `outcomeHandlers` are registries too, so you can add to them later: `sproutForms.validators.register(...)`.

In Nuxt, register on `nuxtApp.$sproutForms` in a plugin of your own, which runs per request:

```ts
// plugins/sproutforms.ts
export default defineNuxtPlugin(nuxtApp => {
    nuxtApp.$sproutForms.validators.register('postcode', value => /^\d{4} ?[A-Z]{2}$/i.test(value));
    nuxtApp.$sproutForms.registerField('starRating', StarRating);
});
```
