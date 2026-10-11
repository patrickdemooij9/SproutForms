# Custom field types

A field type decides what a field holds, how it's validated and how it renders. This page builds a star rating: a number from 1 to a maximum the editor chooses.

A field type needs:

1. Its settings, a plain class.
2. The type itself, built on `FormFieldBase<TConfig, TValue>`.
3. A Razor view at `Views/Forms/Fields/{alias}.cshtml`.
4. Registration, and with Umbraco a descriptor so editors can add it.

For a headless front-end it also needs a control there; see [Custom field types for headless](../extending-headless/field-types.md).

## 1. The settings

```csharp
public class StarRatingFieldConfig
{
    public int MaxStars { get; set; } = 5;
}
```

The settings are stored as JSON with each version of the form, so keep them to plain, serializable properties with setters.

## 2. The field type

```csharp
using SproutForms.Core.Models;

public class StarRatingFieldType : FormFieldBase<StarRatingFieldConfig, int>
{
    public override string Alias => "starRating";

    public override StarRatingFieldConfig DefaultConfiguration => new();

    protected override ValidationResult Validate(int value, StarRatingFieldConfig configuration)
    {
        if (value < 1 || value > configuration.MaxStars)
            return ValidationResult.Fail($"Choose between 1 and {configuration.MaxStars} stars.");

        return ValidationResult.Success();
    }
}
```

- `Alias` identifies the type in stored forms, so don't change it once forms use it. Give it an alias no other field type has: when two types share one, the first registered is used, which is the built-in one. To change how a built-in type renders, replace its view instead (see [Styling](../styling.md#themes-your-own-markup)).
- `TValue` is what a submitted value is converted to before `Validate` gets it: `string`, a number, `bool`, or a class that is deserialized from JSON. A value that can't be converted fails with "Invalid value." without reaching `Validate`.
- `Validate` only runs for a value that isn't empty. Whether a field is required is checked before it, by SproutForms.
- `DefaultConfiguration` is the settings a new field of this type starts with.

`FormFieldBase` has more to override:

| Member | Default | |
|---|---|---|
| `RendersOwnLabel` | `false` | `true` when the field's view renders its own label, like the checkbox; the wrapper then renders none. |
| `GetValidationRulesCore(config)` | none | The rules the browser checks too, see [Custom validators](validators.md). |
| `GetClientConfiguration(config)` | the whole configuration | What the browser sees of the settings, see [Showing settings to the browser](../extending-headless/client-settings.md). |
| `GetDisplayValue(value, configuration, formatter)` | the value as text | How a submitted value shows in emails, workflow messages and the backoffice. |

A field type that needs its own rule for "required", like the checkbox (where "false" is a value but doesn't count), also implements `IFormTypeRequiredHandler`:

```csharp
public ValidationResult CheckForRequired(string value)
    => value == "true" ? ValidationResult.Success() : ValidationResult.Fail("This field is required.");
```

You can implement `IFormFieldType` directly instead of using `FormFieldBase`, but the base class does the conversion and casting for you.

## 3. The Razor view

A field renders with the view `Views/Forms/Fields/{alias}.cshtml`, so put this one in your site at `Views/Forms/Fields/starRating.cshtml`:

```cshtml
@using SproutForms.Umbraco.Core.Models.ViewModels
@model FormFieldViewModel
@{
    var config = (StarRatingFieldConfig)Model.Configuration;
}

<select id="@Model.Alias" name="@Model.Alias" class="form-control">
    <option value=""></option>
    @for (var stars = 1; stars <= config.MaxStars; stars++)
    {
        <option value="@stars" selected="@(Model.Value == stars.ToString())">@stars</option>
    }
</select>
```

Add a `@using` for the namespace of `StarRatingFieldConfig`.

- `Model.Alias` is the field's path: its alias, or in a repeater's entry something like `people[0].rating`. Use it as the input's `name` and `id`: forms.js finds inputs by their `name`, and the label points at the `id`.
- `Model.Value` is the value the visitor posted, when the form is shown again after a post without JavaScript.
- `Model.Configuration` is the field's whole settings object, also what `GetClientConfiguration` leaves out.
- `Field.cshtml` renders the label, the required marker and the errors around this view, unless the type sets `RendersOwnLabel`.

forms.js reads text inputs, selects, textareas, checkboxes (as `true` or `false`), radio buttons and file inputs by their `name`. A control it can't read, such as a custom widget, should keep a hidden input with the field's `name` up to date and dispatch a bubbling `input` or `change` event on it: `input.dispatchEvent(new Event("change", { bubbles: true }))`.

A [theme](../styling.md#themes-your-own-markup) can have its own view of the type at `Views/Forms/Themes/{theme}/Fields/starRating.cshtml`.

## 4. Register it

With Umbraco, register the type and a descriptor in a composer:

```csharp
using Microsoft.Extensions.DependencyInjection;
using SproutForms.Core.Models;
using SproutForms.Umbraco.Core.Descriptors.Fields;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

public class StarRatingFieldDescriptor : BaseFieldDescriptor<StarRatingFieldConfig>
{
    public override string FieldTypeAlias => "starRating";
    public override string DisplayName => "Star rating";
    public override string Icon => "icon-favorite";

    public StarRatingFieldDescriptor()
    {
        DefineMap(it => it.MaxStars, "maxStars", "Number of stars", "Umb.PropertyEditorUi.Integer");
    }
}

public class StarRatingComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddSingleton<IFormFieldType, StarRatingFieldType>();
        builder.Services.AddSingleton<IFieldDescriptor, StarRatingFieldDescriptor>();
    }
}
```

The descriptor's `FieldTypeAlias` must match the type's `Alias`. `DisplayName` and `Icon` (any backoffice icon) are what editors see when they add a field. See [Descriptors](README.md#descriptors) for `DefineMap`.

Without Umbraco, register only the type:

```csharp
builder.Services.AddSingleton<IFormFieldType, StarRatingFieldType>();
```

## Using it in code

`ColumnBuilder.Field` adds a field of any type, with its settings:

```csharp
.Row(row => row.Col(12, col => col
    .Field<StarRatingFieldConfig, int>("rating", "How did we do?", new StarRatingFieldType(), new StarRatingFieldConfig { MaxStars = 10 })
    .Required()
    .Done()))
```

An extension method makes that read like the built-in ones:

```csharp
using SproutForms.Core.Builders;

public static class StarRatingBuilderExtensions
{
    public static FieldBuilder<StarRatingFieldConfig, int> StarRating(this ColumnBuilder column, string alias, string label)
    {
        var fieldType = new StarRatingFieldType();
        return column.Field<StarRatingFieldConfig, int>(alias, label, fieldType, fieldType.DefaultConfiguration);
    }
}
```

```csharp
.Col(12, col => col.StarRating("rating", "How did we do?").Set(c => c.MaxStars = 10).Done())
```

## Only in some form types

A field type that only makes sense in some [form types](form-types.md) implements `IRestrictedToFormTypes`, with the aliases of those types in `FormTypeAliases`. A form type can also exclude field types with `AllowsFieldType`.
