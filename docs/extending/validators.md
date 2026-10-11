# Custom validators

SproutForms validates a submission on the server, always, and in the browser before it's sent, so the visitor sees errors right away. Both sides check the same rules. Validation belongs to a field type: there's no separate validator to register on the server. This page adds a Dutch postcode check.

## How validation works

For every field the visitor can see (fields hidden by their rules, or on a skipped page, aren't validated):

1. A required field that is empty fails with "Field is required.". A field type can change what counts as filled in with `IFormTypeRequiredHandler`, as the checkbox does.
2. A field that isn't empty goes to its field type's `Validate`, which returns a `ValidationResult`.

In the browser, forms.js and the headless packages check the field's **validation rules**, which the field type produces with `GetValidationRulesCore`. A rule is a `type`, a `value` and a `message`. Each rule type needs a validator in the browser; a rule whose type has none is skipped there and left to the server. The server never trusts the browser: it always runs `Validate`.

The built-in rule types are `required`, `minLength`, `maxLength`, `regex`, `minDate`, `maxDate`, `minItems` and `maxItems`.

## A field type with its own rule

```csharp
using System.Text.RegularExpressions;
using SproutForms.Core.Models;

public class PostcodeFieldConfig
{
    public string? Placeholder { get; set; }
}

public class PostcodeFieldType : FormFieldBase<PostcodeFieldConfig, string>
{
    private const string Message = "Enter a postcode like 1234 AB.";
    private static readonly Regex Postcode = new(@"^\d{4} ?[A-Z]{2}$", RegexOptions.IgnoreCase);

    public override string Alias => "postcode";

    public override PostcodeFieldConfig DefaultConfiguration => new();

    // Sent to the browser with the field, for the "postcode" validator there
    protected override IEnumerable<ValidationRule> GetValidationRulesCore(PostcodeFieldConfig config)
    {
        yield return new ValidationRule { Type = "postcode", Message = Message };
    }

    // The server's check, which always runs
    protected override ValidationResult Validate(string value, PostcodeFieldConfig configuration)
        => Postcode.IsMatch(value) ? ValidationResult.Success() : ValidationResult.Fail(Message);
}
```

Register it, give it a view and, with Umbraco, a descriptor, as in [Custom field types](field-types.md).

A rule can carry a `Value` the browser needs, such as the text type's `MaxLength`. Leave settings the browser mustn't see out of the rules, as everything in them is sent to the browser.

## The browser side

Register a validator for the rule type after `forms.js` is loaded. It gets the field's value as text (never empty: empty values only fail "required"), the rule, and a context with the field and the form's values, and returns whether the value is valid:

```js
window.SproutForms.registerValidator("postcode", value => /^\d{4} ?[A-Z]{2}$/i.test(value));
```

The rule's `message` shows when it returns `false`, or "Invalid value." when the rule has none. A validator can be `async`.

Without the validator, the form still works: the server rejects a wrong postcode, and forms.js shows its error after the submit (or, on a [paged form](../forms/pages.md), when the visitor goes to the next page).

For headless front-ends, register the same validator with `@sproutforms/client` or `@sproutforms/vue`; see [Custom validators for headless](../extending-headless/validators.md).

## Rules on the built-in fields

The built-in field types' rules come from their settings: a text field's `MinLength`, `MaxLength` and `Regex`, a date's `Min` and `Max`, a repeater's `MinItems` and `MaxItems`. A text field with a `Regex` is often all a format check needs, without a field type of your own.

## Checks across fields

A check that looks at more than one field, such as "the end date is after the start date", doesn't belong to one field type. A [form type](form-types.md) can do it in `ProcessSubmissionAsync`, which runs after the fields are validated and before the submission is saved, and reject the submission with an error on a field:

```csharp
return Task.FromResult(FormTypeSubmissionResult.Reject("endDate", "The end date must be after the start date."));
```

That check only runs on the server.
