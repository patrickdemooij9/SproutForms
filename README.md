# SproutForms

SproutForms is an open source, free to use forms package for Umbraco. It supports forms in code, forms in the backoffice, various fields and flows that can all be defined either in code or in the backoffice. It also runs on plain ASP.NET Core without Umbraco, and has headless packages for JavaScript front-ends.

# Beta notice
This package is still in beta (and preview in NuGet). This is because I want the package out there to ensure people can start using it, but there are still some things not entirely polished. You can see it as a prototype or MVP which still needs a bit of work/love to get there. By bringing out this beta, you are also able to give feedback on the package and influence the way that the package will be developed.

Another thing is that I'll most likely also introduce a paid package alongside this one. The idea is that the free package provides small websites with an easy to use forms solution that doesn't cost any money. It is also a framework where developers can build/extend upon for their own clients. The paid package will mostly be focused on enterprise and will include the following things at first:
- More flow types focused on integrating with big services.

By letting you know now, I hope it won't feel as a rugpull later on.

# Quick start

1) Install the NuGet package: `Install-Package SproutForms.Umbraco`
2) Add the "SproutForms" section to the user groups that should have it (Administrators get it on install)
3) Add `@addTagHelper *, SproutForms.Core` to your `_ViewImports.cshtml`
4) Add `<render-form-dependencies></render-form-dependencies>` to your `<head>` tag
5) Build a form in the SproutForms section, and render it with `<vc:render-form form-alias="contact"></vc:render-form>` or `<vc:render-form form-id="[guid]"></vc:render-form>`

See [Install with Umbraco](docs/getting-started/umbraco.md) for the details, [Install without Umbraco](docs/getting-started/standalone.md) for a plain ASP.NET Core site, and [Install headless](docs/getting-started/headless/README.md) for a separate front-end.

# Features

- **Field types**: text, email, textarea, checkbox, select (dropdown), radio buttons, date, file upload, hidden, and repeater (a group of fields the visitor fills in more than once).
- **Workflows**: send an email, post to Slack, post to Microsoft Teams, and post to a custom endpoint. They run in the background, in order, and can be retried from the backoffice.
- **Outcomes**: show a message, redirect to a URL or to an Umbraco page.
- **Forms in the backoffice or in code**, with folders, version history and rollback, and recycle bins for forms and submissions.
- **Pages**, **field rules** (show, hide or require a field based on answers) and **calculations** (variables, scores and prices, with conditional outcomes).
- **Validation** in the browser and on the server, from the same rules.
- **Spam protection** with a honeypot field or reCAPTCHA v3.
- **Styling** with CSS custom properties, or your own markup with themes.
- **A headless API** with `@sproutforms/client`, `@sproutforms/vue` and `@sproutforms/nuxt`.
- **Extensible**: your own field types, workflow types, outcome types, submission guards and form types (quizzes, polls and more).

# Documentation

The [documentation](docs/README.md) covers:

- Getting started: [with Umbraco](docs/getting-started/umbraco.md), [without Umbraco](docs/getting-started/standalone.md), [headless](docs/getting-started/headless/README.md) with [Vue](docs/getting-started/headless/vue.md) and [Nuxt](docs/getting-started/headless/nuxt.md)
- Forms: [built-in types](docs/forms/built-in-types.md), [the backoffice](docs/forms/backoffice.md), [code only forms](docs/forms/code-first.md), [pages](docs/forms/pages.md), [field rules](docs/forms/field-rules.md) and [calculations](docs/forms/calculations.md)
- [Styling](docs/styling.md) and [Configuration](docs/configuration.md)
- [Extending SproutForms](docs/extending/README.md) and [Extending for headless front-ends](docs/extending-headless/README.md)
- [Contributing](docs/contributing.md): building, the demo sites and the headless playground
