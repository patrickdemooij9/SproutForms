# Custom submission guards

A submission guard checks every submission before it's validated and saved, to keep bots out. SproutForms comes with two: the honeypot (the default) and reCAPTCHA v3 (see [Configuration](../configuration.md#recaptcha-v3)).

Only one guard is active. Registering another `IFormSubmissionGuard` replaces the honeypot, because the last registration wins.

## The interface

```csharp
public interface IFormSubmissionGuard
{
    string Alias { get; }

    Task<SubmissionGuardResult> EvaluateAsync(Dictionary<string, string> postedValues);
    object? GetFrontendSettings();

    string? PartialViewPath => null;
}
```

- `Alias` names the guard to the browser, which looks up its handler by it.
- `EvaluateAsync` gets every posted value as text: the form's fields, and anything else the form posted, such as the guard's own input or token. Return `new SubmissionGuardResult { Allowed = false, ErrorMessage = "..." }` to reject the submission. The error shows at the top of the form, under the key `submissionGuard`.
- `GetFrontendSettings` is what the browser gets in the definition's `submissionGuard.settings`, such as a site key. It is public, so never put a secret in it.
- `PartialViewPath` is a partial view that Razor forms render inside every `<form>`, for a guard that needs markup, like the honeypot's hidden input.

The guard runs for Razor forms (`/api/forms/{id}`) and for the headless API alike. Headless submissions only pass values that are JSON strings to the guard.

## Example: links on top of the honeypot

Spam often comes with links. This guard rejects a submission with more than two of them, and keeps the honeypot's check by wrapping it. It keeps the honeypot's alias, markup and settings, so the honeypot's browser side keeps working:

```csharp
using SproutForms.Core.Models.SubmissionGuard;

public class LinkLimitSubmissionGuard : IFormSubmissionGuard
{
    private const int MaxLinks = 2;
    private readonly HoneypotSubmissionGuard _honeypot = new();

    public string Alias => _honeypot.Alias;

    public string? PartialViewPath => _honeypot.PartialViewPath;

    public object? GetFrontendSettings() => _honeypot.GetFrontendSettings();

    public async Task<SubmissionGuardResult> EvaluateAsync(Dictionary<string, string> postedValues)
    {
        var honeypot = await _honeypot.EvaluateAsync(postedValues);
        if (!honeypot.Allowed)
            return honeypot;

        var links = postedValues.Values.Sum(value =>
            CountOf(value, "http://") + CountOf(value, "https://"));
        if (links > MaxLinks)
            return new SubmissionGuardResult { Allowed = false, ErrorMessage = "Your message has too many links." };

        return new SubmissionGuardResult { Allowed = true };
    }

    private static int CountOf(string value, string text)
    {
        var count = 0;
        for (var index = value.IndexOf(text, StringComparison.OrdinalIgnoreCase); index >= 0; index = value.IndexOf(text, index + text.Length, StringComparison.OrdinalIgnoreCase))
            count++;
        return count;
    }
}
```

## Register it

With Umbraco, register it in a composer that runs after SproutForms' own, so it comes after the honeypot:

```csharp
using Microsoft.Extensions.DependencyInjection;
using SproutForms.Core.Models.SubmissionGuard;
using SproutForms.Umbraco.Core.Startup;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

[ComposeAfter(typeof(SproutFormComposer))]
public class GuardComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddSingleton<IFormSubmissionGuard, LinkLimitSubmissionGuard>();
    }
}
```

Without Umbraco, register it after `AddSproutFormsStandalone`:

```csharp
builder.Services.AddSproutFormsStandalone(builder.Configuration);
builder.Services.AddSingleton<IFormSubmissionGuard, LinkLimitSubmissionGuard>();
```

To turn guarding off altogether, register `NoFormSubmissionGuard` the same way.

## A guard with a browser side

A captcha needs the browser to do something before the form is sent, such as getting a token from the provider. That takes three pieces:

1. On the server, `GetFrontendSettings` returns what the browser needs, such as the provider's site key, and `EvaluateAsync` checks the value the browser sends, such as by verifying the token with the provider.
2. In the browser, a handler registered under the guard's `Alias`. Its `getValues(settings, values)` returns the values to send with the submission, next to the fields; `EvaluateAsync` finds them in `postedValues`. Its optional `load(settings)` runs when the form shows, to load a script.
3. When the guard needs markup in a Razor form, a partial view in `PartialViewPath`.

```js
// After forms.js is loaded
window.SproutForms.registerSubmissionGuard("myCaptcha", {
    load: async settings => { /* load the provider's script with settings.siteKey */ },
    getValues: async settings => ({ "my-captcha": await getToken(settings.siteKey) })
});
```

`GetFrontendSettings` is serialized to JSON with camelCase names, so `SiteKey` arrives as `settings.siteKey`.

The built-in reCAPTCHA v3 guard is a complete example: [`RecaptchaV3SubmissionGuard.cs`](../../src/SproutForms.Core/Models/SubmissionGuard/RecaptchaV3SubmissionGuard.cs) on the server, and its handler in [`guards.ts`](../../src/SproutForms.Client/src/guards.ts), which forms.js includes. For headless front-ends, see [Custom submission guards for headless](../extending-headless/submission-guards.md).
