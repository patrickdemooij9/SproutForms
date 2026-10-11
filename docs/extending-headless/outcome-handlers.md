# Custom outcome handlers for headless

After a successful submit, the headless API returns the form's outcome: `{ "type": "...", "data": { ... } }`. `type` is the outcome type's alias and `data` is what its `HandleAsync` returned (see [Custom outcome types](../extending/outcome-types.md)). The front-end shows it with the handler registered for that type.

For the built-in types, `data` holds:

| Type | Data |
|---|---|
| `message` | `message`, as HTML written by an editor |
| `redirect` | `url` |
| `redirectUmbracoPage` | `url`, and `path` and `contentKey` for your own router |

When the outcome is `null` (its type isn't registered or it failed), the submission was still saved: show the form's `texts.submitSucceeded`.

## `@sproutforms/client`

No handlers are registered by default: what an outcome looks like is up to your front-end. `handleOutcome(outcome, context)` runs the handler for the outcome's type and returns `false` when there is none:

```ts
import { handleOutcome, registerOutcomeHandler } from '@sproutforms/client';

registerOutcomeHandler('message', outcome => showHtml(String(outcome.data.message)));
registerOutcomeHandler('redirectUmbracoPage', outcome => router.push(String(outcome.data.path)));
registerOutcomeHandler('quizResult', outcome => showScore(Number(outcome.data.score)));

const result = await client.submit(form, { values });
if (result.ok && !await handleOutcome(result.outcome, { definition: form })) {
    showText(form.texts.submitSucceeded);
}
```

The context is whatever you pass to `handleOutcome`, with at least the `definition`, so you can give your handlers more, such as a way to show a message in place of the form.

## `@sproutforms/vue` and `@sproutforms/nuxt`

The built-in `message`, `redirect` and `redirectUmbracoPage` types are handled for you: a message shows in place of the form, and a redirect goes to its URL, in the router when it's on your site. Register handlers for your own types, or to replace the built-in ones. They get `showMessage(html)` and `navigate(url)` on their context:

```ts
createSproutForms({
    client,
    outcomeHandlers: {
        quizResult: (outcome, { showMessage }) => showMessage(`<p>You scored ${Number(outcome.data.score)}</p>`)
    }
});
```

```ts
// Nuxt: plugins/sproutforms.ts
export default defineNuxtPlugin(nuxtApp => {
    nuxtApp.$sproutForms.outcomeHandlers.register('quizResult', (outcome, { showMessage }) =>
        showMessage(`<p>You scored ${Number(outcome.data.score)}</p>`));
});
```

`showMessage` shows the theme's `Success` component with the HTML, so never put values from `data` in it unencoded unless the server made them safe. Without a handler for its type, the form shows its `texts.submitSucceeded`. `<SproutForm>` emits `submitted` with the outcome either way.

The same handler shape works for Razor forms with `window.SproutForms.registerOutcomeHandler`, whose context has the `<form>` element in `form` too.
