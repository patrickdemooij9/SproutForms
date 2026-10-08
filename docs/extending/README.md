# Extending SproutForms

Everything a form is made of is a type you can add to: field types, workflow types, outcome types, form types and the submission guard. Each is a class that implements an interface from `SproutForms.Core`, registered in dependency injection. The built-in ones are registered the same way, so they're good examples: see `src/SproutForms.Core/Fields`, `src/SproutForms.Core/Flows` and `src/SproutForms.Core/Models/Outcomes`.

| To add | Implement | Register as | Backoffice descriptor |
|---|---|---|---|
| [A field type](field-types.md) | `FormFieldBase<TConfig, TValue>` (or `IFormFieldType`) | `IFormFieldType` | `BaseFieldDescriptor<TConfig>`, as `IFieldDescriptor` |
| [A workflow type](workflow-types.md) | `IFormWorkflowType` | `IFormWorkflowType` | `BaseFlowDescriptor<TConfig>`, as `IFlowDescriptor` |
| [Validation rules](validators.md) | `GetValidationRulesCore` and `Validate` on your field type | | |
| [A submission guard](submission-guards.md) | `IFormSubmissionGuard` | `IFormSubmissionGuard` (replaces the honeypot) | |
| [An outcome type](outcome-types.md) | `IFormSubmitOutcomeType` | `IFormSubmitOutcomeType` | `BaseOutcomeDescriptor<TConfig>`, as `IOutcomeDescriptor` |
| [A form type](form-types.md) | `FormDefinitionTypeBase<TSettings>` | `IFormDefinitionType` | `BaseFormDefinitionTypeDescriptor<TSettings>`, as `IFormDefinitionTypeDescriptor` |

This part of the docs is about the server and the Razor forms. A headless front-end needs the browser side of some of these too; see [Extending for headless front-ends](../extending-headless/README.md).

## Registering with Umbraco

Register your types in a composer:

```csharp
using Microsoft.Extensions.DependencyInjection;
using SproutForms.Core.Models;
using SproutForms.Umbraco.Core.Descriptors.Fields;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

public class StarRatingComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddSingleton<IFormFieldType, StarRatingFieldType>();
        builder.Services.AddSingleton<IFieldDescriptor, StarRatingFieldDescriptor>();
    }
}
```

Types are singletons; take what they need in their constructor.

### Descriptors

A type works for code-first forms as soon as it is registered. Editors only see it in the backoffice when it also has a **descriptor**: a class in `SproutForms.Umbraco.Core.Descriptors` that names the type and maps its settings to property editors. The backoffice lists a type only when a descriptor's alias matches the type's alias.

A descriptor maps each setting with `DefineMap` in its constructor:

```csharp
DefineMap(it => it.MaxStars, "maxStars", "Number of stars", "Umb.PropertyEditorUi.Integer");
```

The arguments are the setting, its alias in the backoffice, the label editors see, and the alias of the property editor UI to edit it with. Any Umbraco property editor UI works, such as `Umb.PropertyEditorUi.TextBox`, `Umb.PropertyEditorUi.TextArea`, `Umb.PropertyEditorUi.Integer`, `Umb.PropertyEditorUi.Toggle`, `Umb.PropertyEditorUi.DatePicker`, `Umb.PropertyEditorUi.MultipleTextString` or `Umb.PropertyEditorUi.DocumentPicker`, and SproutForms adds a few of its own:

| Property editor UI | For |
|---|---|
| `SproutForms.KeyValuePair` | A list of label and value pairs, such as a dropdown's options. |
| `SproutForms.FieldOptionPicker` | A dropdown of the edited field's own options, for a [form type](form-types.md)'s field settings. |
| `sproutForms.propertyEditorUi.tokenTextarea` | A text area that suggests `{alias}` tokens, for a workflow's message. |

Field and outcome descriptors' `DefineMap` takes two more, optional arguments: functions that convert a value to what the property editor shows and back, for a setting the editor can't hold as it is. `SelectFieldDescriptor` uses them to turn its options into key-value pairs.

## Registering without Umbraco

Register your types in `Program.cs`, next to SproutForms:

```csharp
using SproutForms.Core.Models;

builder.Services
    .AddSproutFormsStandalone(builder.Configuration)
    .AddSproutFormsInMemoryStorage();
builder.Services.AddSingleton<IFormFieldType, StarRatingFieldType>();
```

There's no backoffice, so there are no descriptors.

## Other extension points

- **Storage**: the repositories in `SproutForms.Core.Repositories`, for a site without Umbraco. See [Install without Umbraco](../getting-started/standalone.md#storage).
- **File storage**: `IFormFileStorageProvider` stores uploads. A file field's `StorageProviderAlias` picks the provider by its `Alias`; the built-in one, `default`, stores them on the local disk.
- **Email**: `IEmailSender` sends the email workflow's emails. With Umbraco it uses Umbraco's email sender; without it, SMTP.
- **Views**: any Razor view of the forms, see [Styling](../styling.md#themes-your-own-markup).
