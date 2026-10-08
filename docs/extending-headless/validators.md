# Custom validators for headless

A field type's validation rules (from `GetValidationRulesCore`, see [Custom validators](../extending/validators.md)) come with each field in the definition, in `validationRules`. A headless front-end checks each rule whose type has a validator, and skips the others; the server checks them all again on submit, and on a [paged form](../forms/pages.md) when the visitor goes to the next page.

## A validator

A validator gets the field's value as text, the rule (`type`, `value` and `message`), and a context with the `field` and the form's `values` (in a repeater's entry, the entry's values over the form's). It returns whether the value is valid, or a promise of it:

```ts
import type { Validator } from '@sproutforms/client';

const postcode: Validator = value => /^\d{4} ?[A-Z]{2}$/i.test(value);

// A rule with a value, such as { type: "maxStars", value: 5 }
const maxStars: Validator = (value, rule) => Number(value) <= Number(rule.value);
```

The value is never empty: an empty field only fails "required". A checkbox's value is `"true"` when it's ticked, an upload's is its file name. When a validator returns `false`, the rule's `message` shows, or the form's `texts.invalid` when it has none.

## Registering it

With `@sproutforms/client`, for every form that doesn't get its own registry:

```ts
import { registerValidator } from '@sproutforms/client';

registerValidator('postcode', value => /^\d{4} ?[A-Z]{2}$/i.test(value));
```

With `@sproutforms/vue`, for one app:

```ts
createSproutForms({
    client,
    validators: { postcode: value => /^\d{4} ?[A-Z]{2}$/i.test(value) }
});
```

With `@sproutforms/nuxt`, in a plugin of your own:

```ts
export default defineNuxtPlugin(nuxtApp => {
    nuxtApp.$sproutForms.validators.register('postcode', value => /^\d{4} ?[A-Z]{2}$/i.test(value));
});
```

The same validator works for Razor forms with `window.SproutForms.registerValidator('postcode', ...)`.

`registerValidator` replaces a validator that has the same type, so it can also change a built-in one: `required`, `minLength`, `maxLength`, `regex`, `minDate`, `maxDate`, `minItems` or `maxItems`. The server keeps checking its own way.

## Validating yourself

With your own renderer, `validateForm(form, values)` validates every field the visitor can see, and `validateForm(form, values, { pageIndex })` one page. Pass `{ validators }` to use a registry of your own. `validateField(field, values, form.texts)` checks one field. The form engine does all of this for you on `next()` and `submit()`.
