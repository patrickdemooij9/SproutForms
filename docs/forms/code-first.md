# Code only forms

A form can be defined in C# instead of in the backoffice. Code-first forms work with Umbraco and without it, and they're the only kind of form a site [without Umbraco](../getting-started/standalone.md) has. They live in source control and are deployed with the site.

## A form

A code-first form is a class that implements `ICodeFirstForm` and builds its definition with `FormBuilder`:

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
            .SubmitLabel("Send")
            .ThankYouMessage("Thanks, we'll be in touch.")
            .OnSubmit(workflows => workflows.SendEmail("notify", email => email
                .To("info@example.com")
                .From("noreply@example.com")
                .Subject("New contact form submission")))
            .Build();
    }
}
```

The class is created with dependency injection, so its constructor can take services, such as `IConfiguration` for an email address.

## Registering it

With Umbraco, in a composer:

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

Without Umbraco, in `Program.cs`:

```csharp
builder.Services
    .AddSproutFormsStandalone(builder.Configuration)
    .AddSproutFormsInMemoryStorage()
    .AddCodeFirstForms(forms => forms.Add<ContactForm>());
```

`AddCodeFirstForms` can be called more than once; the forms add up. `forms.Add(instance)` takes an instance instead of a type.

At startup each form is checked and stored. A new form is created; a form whose definition changed gets a new published version, so its older submissions keep the version they were made with. A form that is invalid (a field on no page, a condition on a field from a later page, a field type the form type doesn't allow) stops the site from starting, with the reason.

A code-first form is found by its alias, so render it with `<vc:render-form form-alias="contact">`. With Umbraco it shows in the backoffice with its submissions, but can't be edited or deleted there.

## Layout

A form is a list of rows, each holding columns on a 12-column grid. Every column holds one field:

```csharp
.Row(row => row
    .Col(4, col => col.Text("postcode", "Postcode").Done())
    .Col(8, col => col.Text("city", "City").Done()))
```

`.Done()` puts the field in its column. Field aliases must be unique across the whole form.

To split the form into steps, add pages instead of rows, see [Pages](pages.md).

## Fields

`ColumnBuilder` has a method per built-in field type: `Text`, `Email`, `Textarea`, `Checkbox`, `Select`, `Radio`, `Date`, `File`, `Hidden` and `Repeater`. Each takes the field's alias and label. See [Built-in fields](built-in-types.md#field-types) for their settings.

On a field:

| Method | Does |
|---|---|
| `Required()` | The field must be filled in. |
| `Set(c => ...)` | Changes the field type's settings, such as `c.Placeholder = "..."` or `c.Options = [...]`. |
| `VisibleWhen(c => ...)`, `HiddenWhen(c => ...)`, `RequiredWhen(c => ...)` | Adds a [field rule](field-rules.md). |
| `Extend(settings)` | Sets the settings a [form type](../extending/form-types.md) adds to this field. |
| `Config` | The field's settings object. |
| `Done()` | Puts the field in its column. |

```csharp
.Col(12, col => col.Radio("contactBy", "How should we contact you?")
    .Set(c => c.Options = [new() { Label = "Email", Value = "email" }, new() { Label = "Phone", Value = "phone" }])
    .Required()
    .Done())
.Col(12, col => col.Text("phone", "Phone number")
    .Set(c => c.Regex = @"^\+?[0-9 ]+$")
    .RequiredWhen(c => c.Field("contactBy").Is("phone"))
    .Done())
```

A field type of your own is added with `Field`, which takes the field type and its settings:

```csharp
col.Field<StarRatingFieldConfig, int>("rating", "Rating", new StarRatingFieldType(), new StarRatingFieldConfig { MaxStars = 10 }).Done()
```

See [Custom field types](../extending/field-types.md).

### Repeaters

A repeater holds fields of its own, laid out in rows, which the visitor fills in as often as its minimum and maximum allow:

```csharp
.Col(12, col => col.Repeater("people", "Attendees", group => group
        .Row(row => row
            .Col(6, c => c.Text("name", "Name").Required().Done())
            .Col(6, c => c.Email("email", "Email").Done())))
    .Set(c => { c.MinItems = 1; c.MaxItems = 5; c.ItemTitle = "Attendee {n}"; c.AddLabel = "Add attendee"; })
    .Done())
```

## Outcomes

What the visitor sees after a successful submit:

| Method | Does |
|---|---|
| `ThankYouMessage(message)` | Shows a message in place of the form. |
| `RedirectTo(url)` | Redirects to a URL. |
| `SetOutcome(alias, configuration)` | Any registered outcome type, such as `"redirectUmbracoPage"` or your own. |
| `SetOutcomeWhen(condition, alias, configuration)` | Uses another outcome when the condition holds, see [Calculations](calculations.md). |

```csharp
.SetOutcome("redirectUmbracoPage", new RedirectUmbracoPageOutcomeConfig { NodeKey = thankYouPageKey })
```

`RedirectUmbracoPageOutcomeConfig` is in `SproutForms.Umbraco.Core.Implementations`, and only exists with Umbraco.

## Workflows

`OnSubmit` adds the workflows that run after a submission is saved, in the order they're added:

```csharp
.OnSubmit(workflows => workflows
    .SendEmail("notify", email => email.To("info@example.com").From("noreply@example.com").Subject("New order {var:total}"))
    .SendToSlack("slack", slack => slack.WebhookUrl("https://hooks.slack.com/services/...").Message("New order:\n{AllValues}"))
    .SendToTeams("teams", teams => teams.WebhookUrl("https://...").Message("New order from {name}"))
    .PostToCustomEndpoint("crm", post => post.Url("https://crm.example.com/api/leads")))
```

Each workflow has an alias of its own, unique in the form. `Add(alias, workflowTypeAlias, configuration)` adds any registered workflow type, such as your own; see [Custom workflow types](../extending/workflow-types.md).

## Everything else

| Method | Does | See |
|---|---|---|
| `Page(title, page => ...)` | Adds a page. | [Pages](pages.md) |
| `SubmitLabel(label)` | The submit button's text. | |
| `ShowProgress(show)` | Whether a form with more than one page shows its progress steps. They show by default. | |
| `Variable(alias, v => ...)` | Adds a variable. | [Calculations](calculations.md) |
| `Calculate(variableAlias, rules => ...)` | Adds calculation rules. | [Calculations](calculations.md) |
| `OfType(alias, settings)` | Makes the form a quiz, poll or other form type. | [Form types](../extending/form-types.md) |

## Examples

The demo site has code-first forms in [`src/SproutForms.Site/Examples`](../../src/SproutForms.Site/Examples): a quiz, a poll and a product finder built on form types, and a calculated quiz, a personality test and a price quote built on calculations. [`src/SproutForms.Standalone.Site/Forms/ContactForm.cs`](../../src/SproutForms.Standalone.Site/Forms/ContactForm.cs) is a contact form without Umbraco.
