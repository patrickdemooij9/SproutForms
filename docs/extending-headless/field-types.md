# Custom field types for headless

A field type you registered on the server (see [Custom field types](../extending/field-types.md)) arrives in the definition like any other field: its `type` is the type's alias, its `configuration` is what `GetClientConfiguration` returns, and its `validationRules` are what `GetValidationRulesCore` returns. A headless front-end only needs a control that renders it.

## Vue and Nuxt

Register a component under the type's alias:

```ts
// Vue
sproutForms.registerField('starRating', StarRating);
// or when creating the plugin
createSproutForms({ client, themes: { default: { fields: { starRating: StarRating } } } });
```

```ts
// Nuxt: plugins/sproutforms.ts
export default defineNuxtPlugin(nuxtApp => {
    nuxtApp.$sproutForms.registerField('starRating', StarRating);
});
```

The control gets `fieldControlProps` and emits `update:modelValue` with the new value. The field wrapper renders the label and the errors around it:

```vue
<script setup lang="ts">
import { computed } from 'vue';
import { fieldControlProps } from '@sproutforms/vue';

interface StarRatingConfiguration {
    maxStars: number;
}

const props = defineProps(fieldControlProps); // field, modelValue, id, path, required, invalid, errors, disabled, describedBy
const emit = defineEmits<{ 'update:modelValue': [value: string] }>();

// The configuration is what the field type's GetClientConfiguration returns, in camelCase
const config = computed(() => props.field.configuration as StarRatingConfiguration);
</script>

<template>
    <select :id="id" class="form-control" :value="modelValue ?? ''" :disabled="disabled"
            :aria-invalid="invalid" :aria-describedby="describedBy"
            @change="emit('update:modelValue', ($event.target as HTMLSelectElement).value)">
        <option value=""></option>
        <option v-for="stars in config.maxStars" :key="stars" :value="String(stars)">{{ stars }}</option>
    </select>
</template>
```

- `modelValue` is the field's value in the form engine. Emit what the server expects to get: the value is sent as JSON, and converted to the field type's `TValue` there.
- `required`, `invalid` and `errors` follow the field's rules and the latest validation.
- `id` is unique on the page; the wrapper's label points at it.
- Set `RendersOwnLabel` on the server type when the control renders its own label; the wrapper then renders none, as for the checkbox.

A field type without a control renders nothing, with a warning in the console. To use a control for one field of one form only, pass it in `<SproutForm :fields="{ rating: StarRating }">`.

## Your own renderer

With `@sproutforms/client` alone, render `field.type === 'starRating'` in your own components, and set its value with `engine.setValue(path, value)` (or keep it in your own values object for `validateForm` and `client.submit`).

A `File` or `Blob` value is sent as an upload: the client switches to `multipart/form-data` by itself.

## Validation

The field's validation rules are checked in the browser when a validator is registered for their type. See [Custom validators for headless](validators.md).
