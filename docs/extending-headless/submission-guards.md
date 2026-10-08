# Custom submission guards for headless

The active submission guard (see [Custom submission guards](../extending/submission-guards.md)) comes with the definition, in `submissionGuard`: its `alias`, and in `settings` what its `GetFrontendSettings` returns. When the form is submitted, the client asks the handler registered for that alias for the values the guard checks, and sends them next to the fields. On the server they arrive in the guard's `EvaluateAsync`.

The honeypot and reCAPTCHA v3 handlers are built into `@sproutforms/client`.

## A handler

```ts
import type { SubmissionGuardHandler } from '@sproutforms/client';

const myCaptcha: SubmissionGuardHandler = {
    // Optional: runs when the form shows, such as to load the provider's script
    async load(settings) {
        await loadScript(`https://captcha.example.com/api.js?key=${encodeURIComponent(String(settings.siteKey))}`);
    },
    // Returns the values the guard checks, sent with the submission
    async getValues(settings, values) {
        return { 'my-captcha': await getToken(String(settings.siteKey)) };
    }
};
```

`settings` is the guard's `GetFrontendSettings`, with camelCase names. `values` is the form's values, for a guard that reads an input of its own, as the honeypot does. `getValues` returns text values only: the server's guard gets every posted value as text, and the headless API only passes values that are strings to it.

## Registering it

Register the handler under the guard's alias:

```ts
// @sproutforms/client
import { registerSubmissionGuard } from '@sproutforms/client';
registerSubmissionGuard('myCaptcha', myCaptcha);

// @sproutforms/vue
createSproutForms({ client, guards: { myCaptcha } });

// @sproutforms/nuxt, in a plugin of your own
nuxtApp.$sproutForms.guards.register('myCaptcha', myCaptcha);
```

The same handler works for Razor forms with `window.SproutForms.registerSubmissionGuard('myCaptcha', myCaptcha)`.

`@sproutforms/vue` calls `load` when a form shows. With your own renderer, call `loadSubmissionGuard(form)` (or `engine.loadSubmissionGuard()`) yourself. `client.submit` and `engine.submit()` call `getValues`. Without a handler for the guard's alias, nothing is added to the submission.

## Guards that need markup

A guard's `PartialViewPath` only renders in Razor forms. A headless front-end renders a guard's markup itself.

The honeypot is one: render a text input named `submissionGuard.settings.fieldName` that people can't see (positioned off-screen, `tabindex="-1"`, `autocomplete="off"`), and keep its value in the form's values under that name. The built-in handler sends it.

`@sproutforms/vue` renders the honeypot's input in its `SubmissionGuard` component. For a guard of your own that needs markup, replace `SubmissionGuard` in a theme, and keep rendering the honeypot there if you still use it.
