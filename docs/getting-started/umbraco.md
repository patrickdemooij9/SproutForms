# Install with Umbraco

`SproutForms.Umbraco` adds a SproutForms section to the Umbraco backoffice, where editors build forms and read their submissions. Forms can also be defined in code. The package targets Umbraco 17 on .NET 10.

## Installation

1) Install the NuGet package: `dotnet add package SproutForms.Umbraco --prerelease` (or `Install-Package SproutForms.Umbraco` in the Package Manager Console). It is still a beta, so it's a prerelease on NuGet.
2) Start the site. SproutForms creates its database tables and adds its section to the Administrators group.
3) Add the "SproutForms" section to any other user groups that should have it.
4) Add the tag helpers to your `Views/_ViewImports.cshtml`:
```cshtml
@addTagHelper *, SproutForms.Core
```
5) Add the stylesheets and script to the `<head>` of your layout:
```cshtml
<render-form-dependencies></render-form-dependencies>
```
Or add the tags themselves:
```html
<link rel="stylesheet" href="/forms/forms-layout.css" />
<link rel="stylesheet" href="/forms/forms-default-theme.css" />
<script src="/forms/forms.js"></script>
```
See [Styling](../styling.md) to leave out the default look or replace the markup.

## Rendering a form

Render a form on a page by its alias or by its id:

```cshtml
<vc:render-form form-alias="contact"></vc:render-form>
<vc:render-form form-id="3f2c1d6e-0000-0000-0000-000000000000"></vc:render-form>
```

The page renders nothing (and logs a warning) when the form doesn't exist or is in the recycle bin. Add `theme="..."` to render it with a theme of your own, see [Styling](../styling.md#themes-your-own-markup).

### Letting editors pick the form

SproutForms has a property editor, the "SproutForms form picker". Create a data type with it and add a property to a document type, so editors choose the form on the page. The property's value is the form's id as a `Guid?`:

```cshtml
@{
    var formId = Model.Value<Guid?>("form");
}
@if (formId.HasValue)
{
    <vc:render-form form-id="@formId"></vc:render-form>
}
```

Replace `"form"` with your property's alias. The same works in a block's view.

## What happens on submit

forms.js validates the form in the browser, with the same rules the server uses, and submits it without a page reload. The server validates it again, checks the submission guard (a honeypot field by default), saves the submission and runs the form's outcome: a message in place of the form, or a redirect. Without JavaScript the form posts as a normal HTML form and gets the same result.

The form's workflows, such as sending an email, run in the background after the submission is saved. Each submission shows how its workflows went, and a failed one can be retried; see [Forms in the backoffice](../forms/backoffice.md#submissions).

## Sending email

The email workflow sends through Umbraco's own email settings, so configure SMTP (or a pickup directory) under `Umbraco:CMS:Global:Smtp`:

```json
"Umbraco": {
  "CMS": {
    "Global": {
      "Smtp": {
        "From": "noreply@example.com",
        "Host": "smtp.example.com",
        "Port": 587,
        "Username": "...",
        "Password": "..."
      }
    }
  }
}
```

The workflow fails, with the reason in the backoffice, while neither SMTP nor a pickup directory is configured.

## Forms in code

Code only forms are registered in a composer:

```csharp
using SproutForms.Core.Registry;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

public class FormsComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddCodeFirstForms(forms => forms.Add<ContactForm>());
    }
}
```

They show up in the backoffice next to the other forms, with their submissions, but they can't be edited or deleted there. See [Code only forms](../forms/code-first.md).

## Next steps

- [Configuration](../configuration.md) for the `SproutForms` settings.
- [Styling](../styling.md) to make the forms look like your site.
- [Install headless](headless/README.md) when a separate front-end renders the forms.
- [Extending SproutForms](../extending/README.md) for your own field types, workflows and more.

`src/SproutForms.Site` in the repository is a working Umbraco site with SproutForms, see [Contributing](../contributing.md).
