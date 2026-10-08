# Custom outcome types

An outcome decides what the visitor sees after a successful submit. The built-in ones show a message or redirect (see [Built-in outcomes](../forms/built-in-types.md#outcomes)). An outcome type of your own runs on the server, with the saved submission, and returns data for the browser. A handler in the browser shows it.

This page builds an outcome that gives the visitor a reference number.

## 1. The outcome type

```csharp
using SproutForms.Core.Models.Outcomes;

public class ReferenceOutcomeConfig
{
    public string Intro { get; set; } = "Thank you. Your reference is";
}

public class ReferenceOutcomeType : IFormSubmitOutcomeType
{
    public string Alias => "reference";

    public Type ConfigurationType => typeof(ReferenceOutcomeConfig);

    public object GetDefaultConfiguration() => new ReferenceOutcomeConfig();

    public Task<OutcomeResult> HandleAsync(FormSubmitOutcomeContext context, CancellationToken cancellationToken)
    {
        var config = (ReferenceOutcomeConfig)context.Configuration;
        var reference = context.Submission.Id.ToString("N")[..8].ToUpperInvariant();

        return Task.FromResult(new OutcomeResult
        {
            Data = new Dictionary<string, object?>
            {
                ["reference"] = reference,
                // Shown as HTML to a visitor without JavaScript
                ["message"] = $"<p>{System.Net.WebUtility.HtmlEncode(config.Intro)} <strong>{reference}</strong>.</p>"
            }
        });
    }
}
```

`HandleAsync` gets a `FormSubmitOutcomeContext`:

- `Configuration`: the outcome's settings on the form, an instance of `ConfigurationType`.
- `Submission`: the saved submission, with its `Values`, the `Variables` the form's [calculations](../forms/calculations.md) worked out, and the `Results` of its [form type](form-types.md).
- `Version`: the form version it was made with.

It returns an `OutcomeResult` whose `Data` goes to the browser. SproutForms sets its `OutcomeTypeAlias` for you. For a visitor without JavaScript, two keys have a meaning of their own:

- `url` redirects to that URL.
- `message` is shown in place of the form, as HTML. Never put submitted values in it unencoded.

Everything in `Data`, these two included, goes to your handler in the browser. The submission is already saved when the outcome runs: an outcome that throws is logged, and the visitor gets the default confirmation.

## 2. The browser side

Register a handler for the outcome's alias after `forms.js` is loaded:

```js
window.SproutForms.registerOutcomeHandler("reference", (outcome, { form, showMessage, navigate }) => {
    const reference = String(outcome.data.reference);
    showMessage(`<p>Thanks! Write down your reference: <strong>${reference.replace(/[^A-Z0-9]/g, "")}</strong></p>`);
});
```

The handler gets the outcome (`type` and `data`) and a context with:

- `form`: the `<form>` element.
- `showMessage(html)`: replaces the form with a success message.
- `navigate(url)`: goes to a URL.
- `definition`: the form's definition.

forms.js only has handlers for the built-in types. For a type without a handler it shows the form's default confirmation, "Thank you, your submission has been received.", so register one for every outcome type of your own. The form also raises `sproutforms:submitted` with the outcome in `detail.outcome`.

For headless front-ends, see [Custom outcome handlers for headless](../extending-headless/outcome-handlers.md).

## 3. Register it

With Umbraco, register the type and a descriptor in a composer:

```csharp
using Microsoft.Extensions.DependencyInjection;
using SproutForms.Core.Models.Outcomes;
using SproutForms.Umbraco.Core.Descriptors.Outcomes;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

public class ReferenceOutcomeDescriptor : BaseOutcomeDescriptor<ReferenceOutcomeConfig>
{
    public override string OutcomeTypeAlias => "reference";
    public override string DisplayName => "Show a reference number";
    public override string Description => "Shows the visitor a reference number for their submission.";

    public ReferenceOutcomeDescriptor()
    {
        DefineMap(it => it.Intro, "intro", "Text before the reference", "Umb.PropertyEditorUi.TextBox");
    }
}

public class ReferenceOutcomeComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddSingleton<IFormSubmitOutcomeType, ReferenceOutcomeType>();
        builder.Services.AddSingleton<IOutcomeDescriptor, ReferenceOutcomeDescriptor>();
    }
}
```

The descriptor's `OutcomeTypeAlias` must match the type's `Alias`. Editors choose the outcome on the form's Settings tab, also as a conditional outcome. See [Descriptors](README.md#descriptors).

Without Umbraco, register only the type:

```csharp
builder.Services.AddSingleton<IFormSubmitOutcomeType, ReferenceOutcomeType>();
```

## Using it in code

```csharp
new FormBuilder("support", "Support")
    .Row(row => row.Col(12, col => col.Textarea("question", "Your question").Required().Done()))
    .SetOutcome("reference", new ReferenceOutcomeConfig { Intro = "We'll answer within a day. Your reference is" })
    .Build();
```

`SetOutcomeWhen(condition, "reference", configuration)` uses it as a [conditional outcome](../forms/calculations.md).

## Only in some form types

An outcome that only makes sense in some [form types](form-types.md), such as a quiz score, implements `IRestrictedToFormTypes` with those types' aliases in `FormTypeAliases`. Editors of other forms can't choose it. A form type can also exclude outcome types with `AllowsOutcomeType`.
