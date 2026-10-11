# Styling

SproutForms ships three files: `forms-layout.css` (the grid, conditional fields and pages), `forms-default-theme.css` (the look) and `forms.js`. They're served from `/forms/`. `<render-form-dependencies></render-form-dependencies>` renders all three. Leave out the ones you don't want:

```cshtml
<render-form-dependencies include-theme="false"></render-form-dependencies>
```

`include-layout`, `include-theme` and `include-scripts` all default to `true`. Or add the tags yourself:

```html
<link rel="stylesheet" href="/forms/forms-layout.css" />
<link rel="stylesheet" href="/forms/forms-default-theme.css" />
<script src="/forms/forms.js"></script>
```

The headless Vue and Nuxt packages render the same markup and use the same two stylesheets, as `@sproutforms/vue/layout.css` and `@sproutforms/vue/theme.css`. Everything on this page about the stylesheets and custom properties applies to them too; see [Vue](getting-started/headless/vue.md#styling) for replacing their components.

## Changing the default theme

The default theme is built on CSS custom properties. Set them on `:root`, or on an element around a form to change only that form:

```css
:root {
    --sf-color-accent: #2e7d32;
    --sf-color-accent-hover: #1b5e20;
    --sf-color-accent-subtle: #e8f5e9;
    --sf-color-focus-ring: rgba(46, 125, 50, 0.2);
    --sf-radius: 0;
}
```

| Property | Default | Used for |
|---|---|---|
| `--sf-font-family` | `system-ui, ...` | All text in the form |
| `--sf-font-size` / `--sf-font-size-small` | `1rem` / `0.875rem` | Inputs and buttons / errors and progress steps |
| `--sf-radius` | `4px` | Inputs and buttons |
| `--sf-control-height` | `2.5rem` | The minimum height of inputs, dropdowns and buttons |
| `--sf-color-text` / `--sf-color-text-muted` | `#222` / `#666` | Text / upcoming progress steps |
| `--sf-color-border` | `#ccc` | Input borders |
| `--sf-color-background` | `#fff` | The Previous button |
| `--sf-color-accent`, `-hover`, `-subtle` | `#1976d2`, `#155fa0`, `#e3f2fd` | Buttons, focus, current progress step |
| `--sf-color-on-accent` | `#fff` | Text on buttons |
| `--sf-color-focus-ring` | `rgba(25, 118, 210, 0.2)` | The ring around a focused input |
| `--sf-color-error` | `#c62828` | Errors, invalid inputs and the required marker |
| `--sf-color-disabled` | `#aaa` | Disabled buttons |
| `--sf-input-padding` / `--sf-button-padding` | `0.5rem 0.6rem` / `0.6rem 1.2rem` | |
| `--sf-repeater-entry-background` / `--sf-repeater-gap` | `#fafafa` / `0.75rem` | A repeater's entries |
| `--sf-progress-track`, `-complete`, `-current` | `#ddd`, `#90caf9`, the accent | Progress steps |
| `--sf-gutter` | `0.5rem` | Space between columns (in `forms-layout.css`) |

To style the forms entirely yourself, leave out `forms-default-theme.css` and style the classes the views render: `form-root`, `form-row`, `form-col col-6`, `form-group`, `form-control`, `form-error`, `form-success` and so on. Keep `forms-layout.css` unless you also handle the grid, hidden fields and pages yourself.

## Themes: your own markup

A theme is a folder under `Views/Forms/Themes/` in your site that only holds the views it changes. Every view it doesn't have comes from the package, so a theme that only changes the text input is one file:

```
Views/Forms/Themes/Bootstrap/Fields/text.cshtml
```

Choose the theme where you render the form, or set a default for the whole site:

```cshtml
<vc:render-form form-alias="contact" theme="Bootstrap"></vc:render-form>
```

```json
"SproutForms": {
  "DefaultTheme": "Bootstrap"
}
```

The views you can put in a theme are `Form`, `Rows`, `Field`, `RepeaterEntry` and `Fields/{field type alias}`; copy the one you want to change from [`src/SproutForms.Core/Views/Forms`](../src/SproutForms.Core/Views/Forms). Render other SproutForms views with `Html.SproutFormsPartialAsync("Field", model)` (from `SproutForms.Core.Rendering`) rather than `Html.PartialAsync`, so they fall back as well. The form root gets `data-sf-theme` with the theme's name, for CSS that belongs to one theme. A theme name can only hold letters, digits, `-` and `_`; any other name is ignored, with a warning in the log, and the default views are used.

The demo site has an example in [`src/SproutForms.Site/Views/Forms/Themes/Example`](../src/SproutForms.Site/Views/Forms/Themes/Example):

```cshtml
@using SproutForms.Core.Rendering
@using SproutForms.Umbraco.Core.Models.ViewModels
@model FormFieldViewModel

<div class="example-field" @AttributesHelper.RenderAttributes(AttributesHelper.Build(Model))>
    @if (!Model.RendersOwnLabel)
    {
        <label class="example-label" for="@Model.Alias">@Model.Label@(Model.Required ? " (required)" : "")</label>
    }

    @await Html.SproutFormsPartialAsync($"Fields/{Model.Type}", Model)

    @if (Model.Errors.Any())
    {
        <p id="@($"{Model.Alias}-error")" class="example-error" data-sf-error role="alert">@string.Join(" ", Model.Errors)</p>
    }
</div>
```

`FormFieldViewModel` is in the `SproutForms.Umbraco.Core.Models.ViewModels` namespace, also in `SproutForms.Core` without Umbraco.

To change a view for every form, whatever the theme, put it at the same path as in the package instead, such as `Views/Forms/Fields/text.cshtml`. That's also where the view of a [custom field type](extending/field-types.md#3-the-razor-view) goes.

## The attributes forms.js needs

forms.js runs the same form engine as the headless front-ends on the form's definition, which `Form.cshtml` writes into the page. It finds the markup by `data-sf-*` attributes, never by class names, so you can use any classes you like (Bootstrap, Tailwind, ...). Keep these when you replace a view:

- The `<form>`: `data-form-ajax`, `data-sf-paged`, the `<script data-sf-definition>` with the definition, and the hidden `data-sf-page-url` input.
- A field's wrapper: the attributes `AttributesHelper.Build` renders, `data-sf-field-id` (the field's path) and `data-sf-field-type`. Inputs are found by their `name`, which is the field's path too.
- A column: `data-sf-col`, so a hidden field hides its column too.
- Pages: `data-sf-page` with the page's index, `data-sf-previous` and `data-sf-next` on the buttons, `data-sf-progress` on the progress list and `data-sf-progress-step` on its items.
- Repeaters: `data-sf-repeater` with the field's path, `data-sf-repeater-entries`, `data-sf-repeater-entry`, `data-sf-entry-title`, the `<template data-sf-repeater-template>` and the `data-sf-repeater-add` and `data-sf-repeater-remove` buttons.

Keep the antiforgery token (`@Html.AntiForgeryToken()`) in `Form.cshtml` too, since the server rejects a post without it, and the loop that renders the submission guard's partial (the honeypot field), or the guard loses its markup.

## State attributes

forms.js marks state with attributes you can style:

| Attribute | On |
|---|---|
| `hidden` | A field and its column hidden by a condition, a page that isn't the current one |
| `data-sf-skipped` | A page skipped by its conditions |
| `aria-current="step"`, `data-sf-complete` | The current and completed progress steps |
| `aria-invalid="true"` | An input with an error |
| `data-sf-error` | An error message (it also has the `form-error` class) |
| `data-sf-global-errors` | The errors that don't belong to a field, at the top of the form (also `form-global-errors`) |
| `data-sf-success` | The confirmation message (also `form-success`) |

## Your own JavaScript

`window.SproutForms` takes the same handlers as the headless client, so one validator or guard works for Razor and headless forms alike. Register them after `forms.js` is loaded:

```js
// A rule type a custom field type returns from GetValidationRules; rules without a validator are only checked by the server
window.SproutForms.registerValidator("postcode", value => /^\d{4} ?[A-Z]{2}$/i.test(value));

// The browser side of a submission guard you registered on the server: the values it checks, sent with the submission
window.SproutForms.registerSubmissionGuard("myCaptcha", { getValues: async settings => ({ "my-captcha": await solve(settings) }) });

// Shows an outcome; the context has the form, showMessage(html) and navigate(url)
window.SproutForms.registerOutcomeHandler("quizResult", (outcome, { showMessage }) => showMessage(`<p>You scored ${Number(outcome.data.score)}</p>`));
```

See [Custom validators](extending/validators.md), [Custom submission guards](extending/submission-guards.md) and [Custom outcome types](extending/outcome-types.md) for the server side of each.

`window.SproutForms.getEngine(form)` gives a form's engine, to read its state or drive it from your own script. The form raises `sproutforms:pagechange` (`detail: { index, previousIndex }`) and `sproutforms:submitted` (`detail: { outcome }`):

```js
document.addEventListener("sproutforms:submitted", e => analytics.track("form submitted", { outcome: e.detail.outcome?.type }));
```
