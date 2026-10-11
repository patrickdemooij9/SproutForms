# Vue: `@sproutforms/vue`

`@sproutforms/vue` renders SproutForms headless forms in Vue 3.5+. A form renders with the same markup and stylesheets as the Razor forms without any code of your own, and you can replace any part of it: one field type, one field, or the form's building blocks per theme.

It is built on [`@sproutforms/client`](README.md#the-javascript-client), whose form engine holds the values, conditions, validation and submit. The Vue components only render the engine's state. For Nuxt, use [`@sproutforms/nuxt`](nuxt.md), which sets all of this up and renders on the server.

> This is a first version. It renders every built-in field type (`text`, `email`, `textarea`, `select`, `radio`, `checkbox`, `date`, `file`, `hidden` and `repeater`) and forms with more than one page, and the honeypot field of the default submission guard.

## Installation

Turn the headless API on in the Umbraco site first, and add your site's origin to `AllowedOrigins`; see [Install headless](README.md#turning-it-on).

```
npm install @sproutforms/vue
```

> `@sproutforms/vue` and `@sproutforms/client` aren't published to npm yet. Until they are, build them from the repository (see [Contributing](../../contributing.md)).

Install the plugin and the stylesheets:

```ts
import { createSproutForms } from '@sproutforms/vue';
import '@sproutforms/vue/layout.css';
import '@sproutforms/vue/theme.css';

app.use(createSproutForms({
    client: { baseUrl: 'https://cms.example.com' }
}));
```

`client` takes the options of [`createSproutFormsClient`](README.md#the-javascript-client), or a client you created yourself.

## Rendering a form

```vue
<SproutForm alias="contact" @submitted="outcome => track(outcome)" />
```

`<SproutForm>` takes:

| Prop | |
|---|---|
| `alias` | The form's id or alias; the form loads its published definition. |
| `definition` | A definition you loaded yourself, instead of `alias`. |
| `theme` | A theme registered on the plugin; the default theme when left out. |
| `fields` | Controls for this form's fields by alias, over their field type's control. |
| `initialValues` | Values by field alias to start with. |

It emits `submitted` with the outcome (or `null`) and the variables the server worked out (those the definition holds), `pagechange` with the new and the previous page index, and `error` when the definition can't be loaded or a submit or page check fails other than on validation.

Errors show once the visitor goes to the next page or submits, and a field's error goes when its value changes. A form with more than one page shows one page at a time, with its progress when the form has `showProgress`. Going to the next page validates the page in the browser, then with the server's rules; pages whose conditions don't hold are skipped. A submit validates the whole form and goes back to the first page with an error. After a submit, a `message` outcome shows in place of the form; `redirect` and `redirectUmbracoPage` go to their URL, in vue-router when it is installed and the URL is on this site.

## Plugin options

`createSproutForms` takes:

| Option | |
|---|---|
| `client` | A client, or the options to create one with. A form that gets its definition passed in only needs one to submit. |
| `themes` | Themes by name. The `default` theme replaces built-in components for every form. |
| `validators` | Validators by rule type, over the global ones. |
| `guards` | Submission guard handlers by guard alias, over the global ones. |
| `outcomeHandlers` | Outcome handlers by outcome type, over the global ones and the built-in `message`, `redirect` and `redirectUmbracoPage` handlers. |
| `navigate` | How a redirect goes to a URL; by default in vue-router when it is installed and the URL is on this site, otherwise by loading it. |
| `loadDefinition` | How `<SproutForm alias>` loads its definition; Nuxt replaces it with `useAsyncData`. |

The plugin also has `registerField`, `registerComponent` and `registerTheme`, to add to it after it is created, and `useSproutFormsPlugin()` gives it to a component.

## Styling

`layout.css` is the grid, `theme.css` the default look, the same files the Razor forms use. Change the theme with the `--sf-*` custom properties, as in [Styling](../../styling.md#changing-the-default-theme), or leave `theme.css` out and style the classes yourself. The markup has the same classes and state attributes as the Razor views: `form-root`, `form-row`, `form-col col-6`, `form-group`, `form-control`, `form-error`, `form-success`, `[hidden]`, `aria-invalid` and so on.

## Replacing parts of the form

Like the Razor views, a form is built from parts you can replace. Each comes from, in this order:

1. The form's `fields` prop, for a field by its alias.
2. The theme the form names.
3. The `default` theme, which every form uses: register into it to change all forms.
4. The built-in component.

### A field type, everywhere

```ts
const sproutForms = createSproutForms({ client, themes: { default: { fields: { textarea: MyTextarea } } } });
// or later, such as from a Nuxt plugin
sproutForms.registerField('textarea', MyTextarea);
```

A field type's control gets `fieldControlProps` and emits `update:modelValue`:

```vue
<script setup lang="ts">
import { fieldControlProps } from '@sproutforms/vue';

const props = defineProps(fieldControlProps); // field, modelValue, id, path, required, invalid, errors, disabled, describedBy
const emit = defineEmits<{ 'update:modelValue': [value: string] }>();
</script>

<template>
    <textarea :id="id" class="form-control" :value="modelValue as string" :aria-invalid="invalid" :aria-describedby="describedBy"
              @input="emit('update:modelValue', ($event.target as HTMLTextAreaElement).value)" />
</template>
```

The field wrapper renders the label, the required marker and the errors around it. A field type that renders its own label, such as the checkbox (`field.rendersOwnLabel`), gets none.

A field type you registered on the server needs a control under its alias, see [Custom field types for headless](../../extending-headless/field-types.md).

### One field of one form

```vue
<SproutForm alias="contact" :fields="{ message: CountedTextarea }" />
```

### A theme

A theme replaces any of the form's building blocks, and what it leaves out comes from the default theme:

```ts
import { createSproutForms, defineTheme } from '@sproutforms/vue';

createSproutForms({
    client,
    themes: {
        compact: defineTheme({
            components: { Field: CompactField },
            fields: { select: FancySelect }
        })
    }
});
```

```vue
<SproutForm alias="contact" theme="compact" />
```

| Component | Is | Gets |
|---|---|---|
| `Form` | The form element with its progress, pages, errors and actions, or the success message once submitted | |
| `Progress` | The steps of a form with more than one page | |
| `Rows` | The rows and columns of a page or a repeater entry | `rows` |
| `Field` | A field's wrapper: label, control and errors | `field` |
| `RepeaterEntry` | One entry of a repeater: its title, rows and remove button. Calls `provideEntryScope` | `field`, `path`, `index`, `removable`; emits `remove` |
| `Actions` | The previous, next and submit buttons | |
| `Errors` | Errors that aren't about one field, such as a failed submit | |
| `SubmissionGuard` | The markup the form's submission guard needs: the honeypot's hidden input. Replace it for a guard of your own that needs markup | |
| `Success` | What shows instead of the form after a `message` outcome | `message` (HTML from the CMS) or `text` |

#### Your own field wrapper

`useField(() => props.field)` gives a `Field` component everything about its field, so yours only does the layout:

```vue
<script setup lang="ts">
import type { FormClientField } from '@sproutforms/client';
import { useField } from '@sproutforms/vue';

const props = defineProps<{ field: FormClientField }>();
const { id, errorId, visible, required, errors, invalid, control, controlProps } = useField(() => props.field);
</script>

<template>
    <div class="my-field" :hidden="!visible || undefined">
        <label v-if="!field.rendersOwnLabel" :for="id">{{ field.label }}{{ required ? ' *' : '' }}</label>
        <component :is="control" v-bind="controlProps" />
        <small v-if="invalid" :id="errorId">{{ errors[0] }}</small>
    </div>
</template>
```

`control` is the field's control as this form resolves it: its alias override, or its type's control in the theme. `controlProps` holds `fieldControlProps` and the update handler. `useField` also returns `path`, `value`, `setValue` and `disabled`. Keep `data-sf-field-type` on the wrapper when you use `layout.css`: it hides a hidden field's column.

In a repeater entry, `useField` gives the field's path in the entry, such as `people[0].email`, and its conditions see the entry's values over the form's. A custom `RepeaterEntry` sets that up with `provideEntryScope(() => props.path, () => props.index)` before it renders the entry's rows; `useFieldScope()` gives the scope a component is in.

#### The rest of the form

Inside any theme component, `useSproutFormContext()` gives you the form: its `definition`, the `engine`, its reactive `state` (values, variables, errors, page index, status), `getFieldId`, `resolveField`, `resolveComponent`, `submit`, `next` and `previous`. The engine adds `addEntry(path)`, `removeEntry(path, index)` and `getVisiblePageIndexes()`. The built-in components in [`src/SproutForms.Vue/src/components/default`](../../../src/SproutForms.Vue/src/components/default) are a good start for your own.

## Validators, guards and outcomes

`createSproutForms` takes `validators`, `guards` and `outcomeHandlers` by key, the same handlers as the client's `registerValidator`, `registerSubmissionGuard` and `registerOutcomeHandler`. They only apply to this app and fall back to the global ones. An outcome handler gets `showMessage(html)` and `navigate(url)` on its context:

```ts
createSproutForms({
    client,
    outcomeHandlers: {
        openChat: (outcome, { showMessage }) => { chat.open(outcome.data.topic); showMessage('<p>We opened a chat for you.</p>'); }
    },
    navigate: url => router.push(url)
});
```

See [Extending for headless front-ends](../../extending-headless/README.md) for each of them.

## Server-side rendering

Create the plugin once per app, so once per request on a server: what you register on it stays in that app. Nothing in the engine or the components touches the DOM while rendering. Without Nuxt, `<SproutForm alias>` loads its definition during server rendering but doesn't hand it to the browser, which loads it again. Pass `definition` with your own state transfer, or pass `loadDefinition` to `createSproutForms`, as the Nuxt module does with `useAsyncData`.

## Playground

`npm run dev:vue` in the repository root starts the Vue playground; see [Contributing](../../contributing.md#vue-and-nuxt).
