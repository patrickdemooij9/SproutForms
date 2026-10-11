# Install without Umbraco

`SproutForms.Core` works on any ASP.NET Core MVC site: code-first forms, rendering, validation, field rules, pages, calculations, outcomes and workflows. There's no backoffice, so forms are defined in code, and there's no headless API (that is part of the Umbraco package).

## Installation

1) Install the NuGet package: `dotnet add package SproutForms.Core --prerelease` (or `Install-Package SproutForms.Core`).
2) Register it in `Program.cs`:
```csharp
using SproutForms.Core;
using SproutForms.Core.Registry;

builder.Services.AddControllersWithViews();
builder.Services
    .AddSproutFormsStandalone(builder.Configuration)
    .AddSproutFormsInMemoryStorage()
    .AddCodeFirstForms(forms => forms.Add<ContactForm>());

// ...
app.MapStaticAssets();
app.MapDefaultControllerRoute();
```
3) Add the tag helpers to your `Views/_ViewImports.cshtml`:
```cshtml
@addTagHelper *, SproutForms.Core
```
4) Add the stylesheets and script to the `<head>` of your layout:
```cshtml
<render-form-dependencies></render-form-dependencies>
```

`MapDefaultControllerRoute` (or any `MapControllers`/`MapControllerRoute`) is needed for the endpoint forms post to, `/api/forms/{id}`. `MapStaticAssets` serves `/forms/forms.js` and the stylesheets.

## A form

A form is a class that implements `ICodeFirstForm`:

```csharp
using SproutForms.Core.Builders;
using SproutForms.Core.Models;

public class ContactForm : ICodeFirstForm
{
    public string Alias => "contact";

    public FormDefinition Build()
    {
        return new FormBuilder("contact", "Contact")
            .Row(row => row
                .Col(6, col => col.Text("name", "Name").Required().Done())
                .Col(6, col => col.Email("email", "Email").Required().Done()))
            .Row(row => row
                .Col(12, col => col.Textarea("message", "Message").Required().Done()))
            .OnSubmit(workflows => workflows.SendEmail("notify", email => email
                .To("info@example.com")
                .From("noreply@example.com")
                .Subject("New contact form submission")))
            .Build();
    }
}
```

See [Code only forms](../forms/code-first.md) for everything a form can hold.

## Rendering a form

```cshtml
<vc:render-form form-alias="contact"></vc:render-form>
```

`form-id="..."` works too, as does `theme="..."`; see [Styling](../styling.md).

## What `AddSproutFormsStandalone` does

It registers everything `AddSproutForms` does (the services, the built-in field, workflow and outcome types, rendering and the honeypot guard), and on top of that:

- registers the code-first forms at startup. An invalid form stops the site from starting, with the reason;
- runs pending workflows in the background, every 10 seconds;
- deletes what has been in the recycle bins longer than `SproutForms:RecycleBin:RetentionDays`, every hour;
- sends emails with SMTP, configured under `SproutForms:Smtp`:

```json
"SproutForms": {
  "Smtp": {
    "Host": "smtp.example.com",
    "Port": 587,
    "EnableSsl": true,
    "UserName": "...",
    "Password": "..."
  }
}
```

Set `PickupDirectory` instead of `Host` to write each email to a folder, such as while developing. Register your own `SproutForms.Core.Models.Flows.Email.IEmailSender` before `AddSproutFormsStandalone` to send emails another way. See [Configuration](../configuration.md#smtp-without-umbraco) for all settings.

## Storage

`AddSproutFormsInMemoryStorage` keeps everything in memory: submissions are gone when the site restarts, and every server of a load balanced site has its own. That's fine when the workflows are what you need from a submission (an email, a webhook), and for trying SproutForms out.

To keep submissions, register your own implementations of the interfaces in `SproutForms.Core.Repositories` instead: `IFormRepository`, `IFormVersionRepository`, `IFormSubmissionRepository`, `IFolderRepository`, `IFormAuditRepository`, `IWorkflowExecutionRepository`, `IWorkflowTemplateRepository` and `IUnitOfWorkProvider`. The in-memory ones in `src/SproutForms.Core/Storage/InMemory` show what each one does.

Uploaded files are stored on disk, under `SproutForms:LocalDiskFileStorage:RootPath`.

## Example

[`src/SproutForms.Standalone.Site`](../../src/SproutForms.Standalone.Site) is a working example. Its emails go to `App_Data/Emails`.

## Next steps

- [Code only forms](../forms/code-first.md), [Pages](../forms/pages.md), [Field rules](../forms/field-rules.md) and [Calculations](../forms/calculations.md).
- [Styling](../styling.md).
- [Extending SproutForms](../extending/README.md): register your own types in `Program.cs`.
