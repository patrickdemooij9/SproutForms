# SproutForms documentation

SproutForms is an open source forms package for Umbraco and plain ASP.NET Core, with headless packages for JavaScript front-ends. Start with the installation article that fits your site, then read on about the forms themselves.

## Getting started

- [Install with Umbraco](getting-started/umbraco.md): the package, the backoffice section and rendering forms on your pages.
- [Install without Umbraco](getting-started/standalone.md): `SproutForms.Core` on any ASP.NET Core MVC site, with forms in code.
- [Install headless](getting-started/headless/README.md): the headless API, its endpoints and the `@sproutforms/client` JavaScript client.
  - [Vue](getting-started/headless/vue.md): `@sproutforms/vue`
  - [Nuxt](getting-started/headless/nuxt.md): `@sproutforms/nuxt`

## Forms

- [Built-in fields, workflows and outcomes](forms/built-in-types.md): what every form can use out of the box.
- [Forms in the backoffice](forms/backoffice.md): building forms, submissions, workflows, history and the recycle bins.
- [Code only forms](forms/code-first.md): forms defined in C# with `FormBuilder`.
- [Pages](forms/pages.md): splitting a form into steps.
- [Field rules](forms/field-rules.md): showing, hiding and requiring fields based on answers.
- [Calculations](forms/calculations.md): variables, calculation rules and conditional outcomes.

## Look and feel

- [Styling](styling.md): the stylesheets, the CSS custom properties, themes with your own markup, and forms.js.

## Configuration

- [Configuration](configuration.md): every `SproutForms` setting in `appsettings.json`.

## Extending

- [Extending SproutForms](extending/README.md): how extensions are registered, for Umbraco and without it.
  - [Custom field types](extending/field-types.md)
  - [Custom workflow types](extending/workflow-types.md)
  - [Custom validators](extending/validators.md)
  - [Custom submission guards](extending/submission-guards.md)
  - [Custom outcome types](extending/outcome-types.md)
  - [Form types](extending/form-types.md): quizzes, polls and other kinds of form.
- [Extending for headless front-ends](extending-headless/README.md): the browser side of the same extensions.
  - [Custom field types](extending-headless/field-types.md)
  - [Custom workflow types](extending-headless/workflow-types.md)
  - [Custom validators](extending-headless/validators.md)
  - [Custom submission guards](extending-headless/submission-guards.md)
  - [Custom outcome handlers](extending-headless/outcome-handlers.md)
  - [Showing settings to the browser](extending-headless/client-settings.md)

## Working on SproutForms

- [Contributing](contributing.md): building the solution, the demo sites and the headless playground.
