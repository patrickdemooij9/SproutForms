# Built-in fields, workflows and outcomes

What every form can use without any code of your own. To add your own, see [Extending SproutForms](../extending/README.md).

## Field types

| Alias | In the backoffice | Code-first | Settings |
|---|---|---|---|
| `text` | Small question | `col.Text(alias, label)` | `Placeholder`, `MinLength`, `MaxLength`, `Regex` |
| `email` | Email | `col.Email(alias, label)` | `Placeholder`. The value must be an email address. |
| `textarea` | Long question | `col.Textarea(alias, label)` | `Rows` (default 5), `MaxLength` |
| `checkbox` | Checkbox | `col.Checkbox(alias, label)` | None. A required checkbox must be ticked. |
| `select` | Dropdown | `col.Select(alias, label)` | `Options` (label and value) |
| `radio` | Radio buttons | `col.Radio(alias, label)` | `Options` (label and value) |
| `date` | Date | `col.Date(alias, label)` | `Min`, `Max`, `IncludeTime` |
| `file` | File | `col.File(alias, label)` | `MaxFileSizeBytes` (default 10 MB), `AllowedExtensions`, `StorageProviderAlias` (default `default`, the local disk) |
| `hidden` | Hidden | `col.Hidden(alias, label)` | `DefaultValue`, `AllowOverrideFromClient` |
| `repeater` | Repeatable block | `col.Repeater(alias, label, group => ...)` | `MinItems`, `MaxItems`, `InitialItems`, `ItemTitle` (`{n}` is the entry's number), `AddLabel`, `RemoveLabel` |

Every field also has a label, can be required, and can have [field rules](field-rules.md).

- A `hidden` field without `AllowOverrideFromClient` always holds its `DefaultValue`, whatever was posted; field rules and calculations see that value.
- Uploads are stored under `SproutForms:LocalDiskFileStorage:RootPath`, see [Configuration](../configuration.md).
- A `repeater` holds fields of its own, laid out in rows like a page, which the visitor fills in as often as its minimum and maximum allow. Entries the visitor leaves empty are dropped. A repeater can't hold another repeater, and field aliases are unique across the whole form. Workflows show a repeater with one `{alias}` token for all its entries; the fields inside it have none.

The settings are on a field's own tab in the backoffice, and set with `.Set(c => ...)` in code:

```csharp
col.Select("topic", "Topic")
    .Set(c => c.Options = [new() { Label = "Sales", Value = "sales" }, new() { Label = "Support", Value = "support" }])
    .Required()
    .Done()
```

## Workflows

Workflows run after a submission is saved, in the background, in the order they're listed: each one waits until the ones before it have succeeded. The backoffice shows how each one went per submission.

| Alias | In the backoffice | Code-first | Settings |
|---|---|---|---|
| `email` | Send an email | `workflows.SendEmail(alias, email => email.To(...).From(...).Subject(...))` | `To`, `From`, `Subject` |
| `slack` | Send to Slack | `workflows.SendToSlack(alias, slack => slack.WebhookUrl(...).Message(...))` | `WebhookUrl`, `Message` |
| `teams` | Send to Microsoft Teams | `workflows.SendToTeams(alias, teams => teams.WebhookUrl(...).Message(...))` | `WebhookUrl`, `Message` |
| `customPost` | Post to custom endpoint | `workflows.PostToCustomEndpoint(alias, post => post.Url(...))` | `Url` |

- **Email** sends an HTML email with every answer, and the variables the form's [calculations](calculations.md) worked out under them. The subject can hold `{alias}` for a field's answer and `{var:alias}` for a variable, on one line. With Umbraco it uses Umbraco's SMTP settings; without, `SproutForms:Smtp` (see [Configuration](../configuration.md)).
- **Slack** and **Teams** post a message to an incoming webhook. The message can hold `{alias}` for a field's answer, `{AllValues}` for all of them and `{var:alias}` for a variable. The default message is "A new submission has been submitted:" followed by `{AllValues}`.
- **Custom POST** posts the submission as JSON: `id`, `formVersionId`, `submittedAt`, `ipAddress`, `pageUrl`, `values` and `variables`.

A built-in workflow that fails for a reason that may pass by itself is tried again later, up to 5 attempts in all (see [Custom workflow types](../extending/workflow-types.md)): an SMTP server or endpoint that can't be reached, a timeout, or an answer of 408, 429 or 5xx. A missing URL, wrong email settings or any other answer fail at once. A failed workflow can always be retried by hand.

In the backoffice, workflows are on a form's Integrations tab. Workflow templates, on the SproutForms overview, hold settings that many forms share, such as the email address to send to. A setting that a template locks always comes from the template.

## Outcomes

An outcome decides what the visitor sees after a successful submit.

| Alias | In the backoffice | Code-first | Data |
|---|---|---|---|
| `message` | Show message | `.ThankYouMessage("...")` or `.SetOutcome(ShowMessageOutcome.Alias, new ShowMessageOutcomeConfig { Message = "..." })` | `message`: shown in place of the form, as HTML |
| `redirect` | Redirect to URL | `.RedirectTo("/thanks")` or `.SetOutcome(RedirectUrlOutcomeType.Alias, new RedirectUrlOutcomeConfig { RedirectUrl = "..." })` | `url` |
| `redirectUmbracoPage` | Redirect to Umbraco Page | `.SetOutcome("redirectUmbracoPage", new RedirectUmbracoPageOutcomeConfig { NodeKey = ... })` | `url`, and `path` and `contentKey` for a headless front-end's router. Umbraco only. |

A form without an outcome shows the message "Thank you for your submission.". A message can hold `{var:alias}` (HTML-encoded) and a redirect URL too (URL-encoded). [Conditional outcomes](calculations.md) replace the form's outcome when their condition holds.

The message is written by an editor and shown as HTML, so it is never filled with submitted values other than variables.

## Submission guards

One submission guard checks every submission before it's saved, to keep bots out:

- **Honeypot** (`honeypot`), the default: a hidden input that people don't see and bots fill in.
- **reCAPTCHA v3** (`recaptchaV3`): Google's score-based check. Turn it on as in [Configuration](../configuration.md#recaptcha-v3).

To write your own, see [Custom submission guards](../extending/submission-guards.md).
