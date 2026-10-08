# Showing settings to the browser

A form's definition goes to the browser: in the headless API's `GET definitions/{idOrAlias}`, and written into the page for a Razor form's forms.js. It never holds what a visitor mustn't see: the storage provider of an upload, workflows, the outcome's settings, the calculations of variables no condition uses, or a form type's settings and field extensions (such as a quiz's correct answers).

What a custom type contributes to it is up to the type.

## A field type's settings

A field type decides what the browser sees of its configuration with `GetClientConfiguration`. By default that's the whole configuration, so override it when yours holds something only the server needs:

```csharp
protected override object? GetClientConfiguration(FileFieldConfig configuration)
    => new FileFieldClientConfig { MaxFileSizeBytes = configuration.MaxFileSizeBytes, AllowedExtensions = configuration.AllowedExtensions };
```

That's the built-in file type, which leaves out its storage provider. Return `null` to send nothing. The result is serialized to JSON with camelCase names, and is a field's `configuration` in the definition. Razor views still get the whole configuration.

Validation rules from `GetValidationRulesCore` go to the browser too, so don't put a secret in a rule's `value`.

## A form type's settings

A form type shows nothing of its settings or field extensions unless it overrides `GetClientSettings(definition)` or `GetClientFieldExtension(field)`:

```csharp
public class QuizFormType : FormDefinitionTypeBase<QuizSettings>
{
    // ...

    // The definition's formTypeSettings: only what the front-end needs, such as to show the pass mark above the quiz
    public override object? GetClientSettings(FormDefinition definition)
        => new { GetSettings(definition).PassMark };

    // A field's extension: the points a question is worth, never its correct answer
    public override object? GetClientFieldExtension(FormField field)
        => new { GetFieldSettings<QuizAnswerSettings>(field).Points };
}
```

`QuizFormType` and its settings are the ones from [Form types](../extending/form-types.md).

`GetClientSettings` becomes the definition's `formTypeSettings`, next to `formType`, the type's alias. `GetClientFieldExtension` becomes a field's `extension`. Razor views still get everything.

## A submission guard's settings

`GetFrontendSettings` is the definition's `submissionGuard.settings`, such as reCAPTCHA's site key. It's public, so never put the secret key in it. See [Custom submission guards](../extending/submission-guards.md).

## Variables

A variable stays on the server, with its rules, unless a page condition or field rule uses it. Then the browser gets it, and the rules it needs, to decide what to show. A headless submit returns the values of the variables the browser got in `variables`. See [Calculations](../forms/calculations.md).
