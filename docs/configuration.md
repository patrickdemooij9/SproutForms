# Configuration

Settings go in a `SproutForms` section in `appsettings.json` (or any other configuration source, such as environment variables like `SproutForms__StoreIpAddress`). All of them are optional.

```json
"SproutForms": {
  "StoreIpAddress": false,
  "DefaultTheme": "",
  "RecycleBin": {
    "RetentionDays": 30
  },
  "LocalDiskFileStorage": {
    "RootPath": "App_Data/SproutForms/Uploads"
  },
  "Headless": {
    "Enabled": false,
    "AllowedOrigins": [],
    "ApiKey": ""
  },
  "RecaptchaV3": {
    "SiteKey": "",
    "SecretKey": "",
    "MinimumScore": 0.5,
    "Action": "submit"
  },
  "Smtp": {
    "Host": "",
    "Port": 25,
    "EnableSsl": false,
    "UserName": "",
    "Password": "",
    "PickupDirectory": ""
  }
}
```

## General

| Setting | Default | |
|---|---|---|
| `StoreIpAddress` | `false` | Stores the visitor's IP address with each submission. |
| `DefaultTheme` | none | The [theme](styling.md#themes-your-own-markup) forms render with when the page doesn't choose one: a folder under `Views/Forms/Themes/`. Empty uses the default views. |
| `RecycleBin:RetentionDays` | `30` | Days a form or submission stays in the recycle bin before it's deleted for good. `0` keeps it until someone deletes it. |
| `LocalDiskFileStorage:RootPath` | `App_Data/SproutForms/Uploads` | Where uploaded files are stored, relative to the site's content root. |

An IP address is personal data under the GDPR, so only turn `StoreIpAddress` on when you have a reason to keep it. Behind a proxy or load balancer, configure ASP.NET Core's forwarded headers middleware, or you store the proxy's address instead of the visitor's.

Submissions hold personal data too, so they shouldn't stay in the recycle bin forever. The bins are emptied by a job that runs every hour. See [Forms in the backoffice](forms/backoffice.md#recycle-bins).

## Headless

Umbraco only. See [Install headless](getting-started/headless/README.md#turning-it-on).

| Setting | Default | |
|---|---|---|
| `Headless:Enabled` | `false` | Turns on the headless API under `/umbraco/sproutforms/delivery/api/v1`. While it's off, the endpoints answer 404. Changing it needs a restart for CORS and the OpenAPI document. |
| `Headless:AllowedOrigins` | none | The origins (such as `https://www.example.com`) of the front-ends that call the API from a browser. They're allowed by CORS, and a submission's page URL is only stored when it is on this site or one of these origins. |
| `Headless:ApiKey` | none | When set, every headless request must send it in the `Api-Key` header. Only for a front-end that calls the API from its server; a key in browser code is public. |

## reCAPTCHA v3

The honeypot is the default submission guard. To use Google reCAPTCHA v3 instead, turn it on and set its keys:

| Setting | Default | |
|---|---|---|
| `RecaptchaV3:SiteKey` | | The site key, sent to the browser. |
| `RecaptchaV3:SecretKey` | | The secret key, only used on the server. Without it every submission is rejected. |
| `RecaptchaV3:MinimumScore` | `0.5` | The lowest score Google may give a submission. |
| `RecaptchaV3:Action` | `submit` | The action the browser asks a token for; the token's action must match it. |

With Umbraco, turn it on with `EnableSproutFormsRecaptchaV3()` in a composer that runs after SproutForms' own, since only one guard is active and the last one registered wins:

```csharp
using SproutForms.Umbraco.Core.Extensions;
using SproutForms.Umbraco.Core.Startup;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

[ComposeAfter(typeof(SproutFormComposer))]
public class RecaptchaComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder) => builder.EnableSproutFormsRecaptchaV3();
}
```

Without Umbraco, register the guard and its settings after `AddSproutFormsStandalone`:

```csharp
using SproutForms.Core.Models.SubmissionGuard;

builder.Services.AddSproutFormsStandalone(builder.Configuration);
builder.Services.AddSingleton<IFormSubmissionGuard, RecaptchaV3SubmissionGuard>();
builder.Services.Configure<RecaptchaV3Options>(builder.Configuration.GetSection("SproutForms:RecaptchaV3"));
```

forms.js and `@sproutforms/client` load the reCAPTCHA script and send the token with each submission themselves.

## SMTP without Umbraco

Only used by `AddSproutFormsStandalone`. With Umbraco, the email workflow uses Umbraco's own `Umbraco:CMS:Global:Smtp` settings instead.

| Setting | Default | |
|---|---|---|
| `Smtp:Host` | | The SMTP server. |
| `Smtp:Port` | `25` | |
| `Smtp:EnableSsl` | `false` | |
| `Smtp:UserName` / `Smtp:Password` | | Leave them out for a server without authentication. |
| `Smtp:PickupDirectory` | | Writes each email as a file to this folder instead of sending it, such as while developing. Relative to the content root. |

Set either `Host` or `PickupDirectory`; with neither, the email workflow fails.
