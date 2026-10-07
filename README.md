# SproutForms

SproutForms is an open source, free to use Forms package for Umbraco. It supports forms in code, forms in the backoffice, various fields and flows that can all be defined either in code or in the backoffice.

# Beta notice
This package is still in beta (and preview in NuGet). This is because I want the package out there to ensure people can start using it, but there are still some things not entirely polished. You can see it as a prototype or MVP which still needs a bit of work/love to get there. By bringing out this beta, you are also able to give feedback on the package and influence the way that the package will be developed.

Another thing is that I'll most likely also introduce a paid package alongside this one. The idea is that the free package provides small websites with an easy to use forms solution that doesn't cost any money. It is also a framework where developers can build/extend upon for their own clients. The paid package will mostly be focused on enterprise and will include the following things at first:
- More flow types focused on integrating with big services.

By letting you know now, I hope it won't feel as a rugpull later on.

# Installation

Installation is simple with just a few steps being required.

1) Download the Nuget package `Install-Package SproutForms.Umbraco`
2) Add the "SproutForms" section to the user/user groups that you want to have this functionality
3) Add `<render-form-dependencies></render-form-dependencies>` to your <head> tag (see [Styling](#styling)), or the tags themselves:
```
<link rel="stylesheet" href="/forms/forms-layout.css" />
<link rel="stylesheet" href="/forms/forms-default-theme.css" />
<script src="/forms/forms.js"></script>
```
4) Add the following to your "_ViewImports.cshtml": `@addTagHelper *, SproutForms.Core`
5) You can now render your forms by using `<vc:render-form form-alias="testFileForm"></vc:render-form>` or `<vc:render-form form-id="[guid]"></vc:render-form>`

## Without Umbraco

`SproutForms.Core` works on any ASP.NET Core MVC site: code-first forms, rendering, validation, conditions, calculations, outcomes and workflows. There's no backoffice, so forms are defined in code.

1) Download the NuGet package `Install-Package SproutForms.Core`
2) Register it in `Program.cs`:
```csharp
builder.Services.AddControllersWithViews();
builder.Services
    .AddSproutFormsStandalone(builder.Configuration)
    .AddSproutFormsInMemoryStorage()
    .AddCodeFirstForms(forms => forms.Add<ContactForm>());

// ...
app.MapStaticAssets();
app.MapDefaultControllerRoute();
```
3) Add `@addTagHelper *, SproutForms.Core` to your "_ViewImports.cshtml", and render forms as in steps 3 and 5 above.

`AddSproutFormsStandalone` registers the code-first forms at startup, runs the workflows and empties the recycle bin in the background, and sends emails with SMTP:

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

Set `PickupDirectory` instead of `Host` to write each email to a folder, such as while developing.

`AddSproutFormsInMemoryStorage` keeps everything in memory: submissions are gone when the site restarts, and every server of a load balanced site has its own. That's fine when the workflows are what you need from a submission (an email, a webhook), and for trying SproutForms out. To keep submissions, register your own implementations of the interfaces in `SproutForms.Core.Repositories` instead.

`src/SproutForms.Standalone.Site` is a working example.

## Configuration

Settings go in a `SproutForms` section in `appsettings.json`. All of them are optional:

```json
"SproutForms": {
  "StoreIpAddress": false,
  "LocalDiskFileStorage": {
    "RootPath": "App_Data/SproutForms/Uploads"
  }
}
```

- `StoreIpAddress` (default `false`) stores the visitor's IP address with each submission. An IP address is personal data under the GDPR, so only turn this on when you have a reason to keep it. Behind a proxy or load balancer, configure ASP.NET Core's forwarded headers middleware, or you store the proxy's address instead of the visitor's.
- `LocalDiskFileStorage:RootPath` is where uploaded files are stored, relative to the site's content root.

## Styling

SproutForms ships three files: `forms-layout.css` (the grid, conditional fields and pages), `forms-default-theme.css` (the look) and `forms.js`. `<render-form-dependencies></render-form-dependencies>` renders all three. Leave out the ones you don't want:

```
<render-form-dependencies include-theme="false"></render-form-dependencies>
```

`include-layout`, `include-theme` and `include-scripts` all default to `true`.

### Changing the default theme

The default theme is built on CSS custom properties. Set them on `:root`, or on an element around a form to change only that form:

```css
:root {
    --sf-color-accent: #2e7d32;
    --sf-color-accent-hover: #1b5e20;
    --sf-color-accent-subtle: #e8f5e9;
    --sf-color-focus-ring: rgba(46, 125, 50, 0.2);
    --sf-radius: 0;
}
```

| Property | Default | Used for |
|---|---|---|
| `--sf-font-family` | `system-ui, ...` | All text in the form |
| `--sf-font-size` / `--sf-font-size-small` | `1rem` / `0.875rem` | Inputs and buttons / errors and progress steps |
| `--sf-radius` | `4px` | Inputs and buttons |
| `--sf-control-height` | `2.5rem` | The minimum height of inputs, dropdowns and buttons |
| `--sf-color-text` / `--sf-color-text-muted` | `#222` / `#666` | Text / upcoming progress steps |
| `--sf-color-border` | `#ccc` | Input borders |
| `--sf-color-background` | `#fff` | The Previous button |
| `--sf-color-accent`, `-hover`, `-subtle` | `#1976d2`, `#155fa0`, `#e3f2fd` | Buttons, focus, current progress step |
| `--sf-color-on-accent` | `#fff` | Text on buttons |
| `--sf-color-focus-ring` | `rgba(25, 118, 210, 0.2)` | The ring around a focused input |
| `--sf-color-error` | `#c62828` | Errors, invalid inputs and the required marker |
| `--sf-color-disabled` | `#aaa` | Disabled buttons |
| `--sf-input-padding` / `--sf-button-padding` | `0.5rem 0.6rem` / `0.6rem 1.2rem` | |
| `--sf-progress-track`, `-complete`, `-current` | `#ddd`, `#90caf9`, the accent | Progress steps |
| `--sf-gutter` | `0.5rem` | Space between columns (in `forms-layout.css`) |

### Themes: your own markup

A theme is a folder under `Views/Forms/Themes/` in your site that only holds the views it changes. Every view it doesn't have comes from the package, so a theme that only changes the text input is one file:

```
Views/Forms/Themes/Bootstrap/Fields/text.cshtml
```

Choose the theme where you render the form, or set a default for the whole site:

```
<vc:render-form form-alias="contact" theme="Bootstrap"></vc:render-form>
```

```json
"SproutForms": {
  "DefaultTheme": "Bootstrap"
}
```

The views you can put in a theme are `Form`, `Rows`, `Field` and `Fields/{field type alias}`; copy the one you want to change from [`src/SproutForms.Core/Views/Forms`](src/SproutForms.Core/Views/Forms). Render other SproutForms views with `Html.SproutFormsPartialAsync("Field", model)` (from `SproutForms.Core.Rendering`) rather than `Html.PartialAsync`, so they fall back as well. The form root gets `data-sf-theme` with the theme's name, for CSS that belongs to one theme. A theme name can only hold letters, digits, `-` and `_`. The demo site has an example in `src/SproutForms.Site/Views/Forms/Themes/Example`.

To change a view for every form, whatever the theme, put it at the same path as in the package instead, such as `Views/Forms/Fields/text.cshtml`.

forms.js only relies on `data-sf-*` attributes, never on class names, so you can use any classes you like (Bootstrap, Tailwind, ...). Keep these attributes when you replace a view:

- The `<form>`: `data-form-ajax`, `data-submission-guards`, `data-sf-paged`, `data-sf-calculations`, and the hidden `data-sf-page-url` input.
- A field's wrapper: the attributes `AttributesHelper.Build` renders (`data-sf-field-id`, `data-sf-validate`, ...) and `data-field-rules`. Inputs are found by their `name`.
- A column: `data-sf-col`, so a hidden field hides its column too.
- Pages: `data-sf-page` and its labels and conditions, `data-sf-previous` and `data-sf-next` on the buttons, `data-sf-progress` on the progress list and `data-sf-progress-step` on its items.

forms.js marks state with attributes you can style:

| Attribute | On |
|---|---|
| `hidden` | A field and its column hidden by a condition, a page that isn't the current one |
| `data-sf-skipped` | A page skipped by its conditions |
| `aria-current="step"`, `data-sf-complete` | The current and completed progress steps |
| `aria-invalid="true"` | An input with an error |
| `data-sf-error` | An error message (it also has the `form-error` class) |
| `data-sf-global-errors` | The errors that don't belong to a field, at the top of the form (also `form-global-errors`) |
| `data-sf-success` | The confirmation message (also `form-success`) |

## Contents

SproutForms currently supports these field types:
- Textbox (small question)
- Textarea (larger question)
- Email
- Date
- File
- Select (dropdown)
- Radio buttons
- Hidden

And supports the following flows:
- Send email

Data is automatically stored in the database and can be viewed as such in the backoffice. You also have the option to either show a message or redirect the user to a different page.

## Pages

A form can be split into pages. forms.js shows one page at a time with Previous and Next buttons, and a progress list of the page titles. Next checks the current page's fields before it moves on, first in the browser and then on the server (`POST /api/forms/{id}/pages/{index}/validate`), so rules only the server checks, such as the email format, show on the page they belong to. Uploads are only checked when the form is submitted. The form is still submitted once, from the last page, and an error on an earlier page takes the visitor back to it. Without JavaScript every page shows, one after the other.

In the backoffice, the Build tab shows the form's pages above the canvas. Click a page to edit its fields and, in the side panel, its title, button labels and when it's shown. Drop a field on a page to move it there, or drag a page to reorder. The submit button's text and whether the progress steps show are on the Settings tab.

A form is checked when it's saved, and when a code-first form is registered: every field is placed on exactly one page, only a form's single page may be empty, and a condition only uses fields from earlier pages (or, for a field, its own page).

In code, add each page with `Page`. A page without a title shows as "Step n" in the progress list:

```csharp
new FormBuilder("order", "Order")
    .Page("About you", page => page
        .NextLabel("Continue")
        .Row(row => row.Col(12, col => col.Text("name", "Name").Required().Done())))
    .Page("Your order", page => page
        .PreviousLabel("Back")
        .Row(row => row.Col(12, col => col.Textarea("message", "Message").Done())))
    .SubmitLabel("Send order")
    .Build();
```

A page can depend on earlier answers with `VisibleWhen`. When its conditions don't hold, the page is skipped and left out of the progress list, and its fields aren't validated, in the browser or on the server:

```csharp
.Page("Delivery address", page => page
    .VisibleWhen(c => c.Field("delivery", ConditionComparison.Equals, "home"))
    .Row(row => row.Col(12, col => col.Text("address", "Address").Required().Done())))
```

A form built with only `Row` has a single page, and renders without page navigation. Every page change raises a `sproutforms:pagechange` event on the form, with the new and previous page index in `detail`:

```js
document.addEventListener("sproutforms:pagechange", e => window.scrollTo({ top: e.target.offsetTop }));
```

## Field rules

A field's rules say when it shows, hides or is required: each rule has a condition and an action. A field is hidden while any of its Hide rules holds and, when it has Show rules, only shown while one of them holds. It is required while any of its Require rules holds. A hidden field isn't validated, in the browser or on the server. A rule can also change a variable, see [Calculations](#calculations).

In the backoffice, a field's rules are on its Rules tab. In code, `VisibleWhen`, `HiddenWhen` and `RequiredWhen` each add a rule:

```csharp
.Col(12, col => col.Text("address", "Address")
    .VisibleWhen(c => c.Field("delivery").Is("home"))
    .RequiredWhen(c => c.Field("delivery").Is("home"))
    .Done())
```

## Calculations

A form can work out values from its answers, such as a quiz score, a personality type or a price, without any code. It has **variables**, each a number or text with a starting value, and **calculation rules** that change them. A rule has an optional condition and an operation: a number can be set, added to, subtracted from, multiplied or divided, and text can be set or appended to. The value a rule works with is a value you type, or the value of a field or another variable. The rules run top to bottom, so a rule sees what the rules above it did.

- A field the visitor doesn't see, because of its rules or a skipped page, counts as empty. A value that isn't a number counts as 0, and dividing by zero leaves a variable as it was.
- A number is rounded to its variable's decimals once all rules have run.
- Conditions can compare a field or a variable with a value, another field or another variable. For "the highest counter wins", compare the counters: `When introvert > extrovert, set personality to "Introvert"`.
- A calculation rule can be listed on a field, among the field's own rules ("when this answer is Paris, add 10 to score"). It still runs in its place in the form's list of rules.
- Page conditions and field rules can use variables too, to skip a page or show a field based on the answers so far. A variable a condition uses may only depend on earlier pages, and a field's visibility can't depend on the field itself; saving such a form fails with a message that says which variable and field.
- **Conditional outcomes** replace the form's outcome when their condition holds, such as a different message or result page per score range. They're checked top to bottom, and the first that holds is used; otherwise the form's own outcome is.
- `{var:alias}` fills in a variable's value in a message outcome (HTML-encoded), a redirect URL (URL-encoded), an email subject, and Slack and Teams messages. A number shows its variable's decimals. Emails list the variables under the answers, and a Custom POST sends them as `variables`.
- The server always works out the variables again when a form is submitted, whatever the browser sent, and stores them with the submission. The backoffice shows them with each submission, and as a column in the list.
- A variable stays on the server, with its rules, unless a page condition or field rule uses it. Then the browser gets it, and the rules it needs, to decide what to show. So a quiz score only reaches the browser when a condition uses it. A headless submit returns the values of the variables the browser got in `variables`.

In the backoffice, variables and all calculation rules are under Settings → Calculations, conditional outcomes under Settings → Outcomes, and a field's rules on its Rules tab. In code:

```csharp
new FormBuilder("quiz", "Coffee quiz")
    .Row(row => row.Col(12, col => col.Text("capital", "Capital of Italy").Done()))
    .Row(row => row.Col(12, col => col.Text("beans", "Most grown bean").Done()))
    .Variable("score", v => v.Label("Score"))
    .Calculate("score", rules => rules
        .When(c => c.Field("capital").Is("Rome")).Add(10)
        .When(c => c.Field("beans").Is("arabica")).Add(5))
    .SetOutcome(ShowMessageOutcome.Alias, new ShowMessageOutcomeConfig { Message = "You scored {var:score}." })
    .SetOutcomeWhen(c => c.Variable("score").Is(15), ShowMessageOutcome.Alias, new ShowMessageOutcomeConfig { Message = "Perfect!" })
    .Build();
```

`Variable` takes `Number(decimals)`, `Text()` and `StartAt(value)`. A rule's value can be `ValueOf.Field("quantity")` or `ValueOf.Variable("price")`, in conditions too: `c.Variable("latte").GreaterThan(ValueOf.Variable("espresso"))`. The demo site has a calculated quiz, a personality test with a tie-breaker page, and a price quote in [`src/SproutForms.Site/Examples/Calculations`](src/SproutForms.Site/Examples/Calculations).

A form type's `ProcessSubmissionAsync` gets the variables in `context.Variables`, and an outcome finds them in `context.Submission.Variables`.

# Headless

A front-end that isn't rendered by Umbraco, such as a Next.js, Nuxt or mobile app, can use SproutForms through its headless API. The API returns a published form's structure as JSON and accepts submissions. The front-end renders the form itself.

> The headless API is new, and it may still change until SproutForms 1.0. Its routes are versioned (`/v1/`).

## Turning it on

The headless API is off by default:

```json
"SproutForms": {
  "Headless": {
    "Enabled": true,
    "AllowedOrigins": [ "https://www.example.com" ],
    "ApiKey": ""
  }
}
```

- `Enabled`: turns on the endpoints. While it's off, they answer 404. Changing it needs a restart for CORS and the OpenAPI document.
- `AllowedOrigins`: the front-ends that call the API from a browser. They're allowed by CORS. A submission's page URL (`sf_PageUrl`) is only stored when it's on this site or on one of these origins; otherwise it's dropped.
- `ApiKey`: when set, every request must send it in the `Api-Key` header. Only use it when your front-end calls the API from its own server. A key in browser code is public.

The API has no antiforgery token, since the front-end runs on another origin. The submission guard keeps bots out instead. That's a honeypot field by default, or reCAPTCHA v3 with `builder.EnableSproutFormsRecaptchaV3()` and the `SproutForms:RecaptchaV3` settings. A bot can post to the API directly and leave the honeypot empty, so use reCAPTCHA for a form that attracts spam.

## Endpoints

All routes start with `/umbraco/sproutforms/delivery/api/v1`. The OpenAPI document is at `/umbraco/swagger/sproutforms-delivery/swagger.json`.

| Endpoint | Does |
|---|---|
| `GET definitions/{idOrAlias}` | The published form: its pages, rows and columns, each column holding its field with its configuration, rules and validation rules (the same shape as the Razor view model), the submission guard's settings and the default texts. The `ETag` is the published version, so a client can revalidate with `If-None-Match` (304). |
| `POST entries/{id}` | Submits the form. The body is its values as a JSON object, the same values a Razor form posts: `{ "alias": value, "sf_PageUrl": "...", ... }`, with what the submission guard checks next to the fields. A repeater's value is a list of entry objects, `[{ "alias": value }]`. With uploads, send `multipart/form-data` instead: the values as a JSON part named `values`, and each file as a part named by its field's path, such as `cv` or `people[0].cv`. Returns `{ "outcome": { "type", "data" }, "variables": { ... } }`, with the values of the variables the definition holds (those its conditions use). |
| `POST entries/{id}/pages/{index}/validate` | Checks one page of a paged form with the values entered so far, as the same JSON object, without saving anything. Returns 204 when the page is valid. |

A rejected submission or page returns 400 as `application/problem+json`, with the errors keyed by field alias in `errors`. `submissionGuard`, and any other key that isn't a field, is about the whole form.

The values also hold what the submission guard checks: `"g-recaptcha-response"` with the token for reCAPTCHA, or the honeypot's field (its name is in `submissionGuard.settings.fieldName`) with whatever that hidden input holds. The client adds them, and the page URL, for you.

The outcome's `data` depends on its type:
- `message` has `message`.
- `redirect` has `url`.
- `redirectUmbracoPage` has `url`, `path` and `contentKey`, for your own router.

When `outcome` is null, the submission was still saved. Show the form's `texts.submitSucceeded`.

The definition never holds what a visitor mustn't see: the storage provider of an upload, workflows, the outcome's settings, the calculations of variables no condition uses, or a form type's settings and field extensions (such as a quiz's correct answers). See [Showing settings to a headless front-end](#showing-settings-to-a-headless-front-end).

## The JavaScript client

[`@sproutforms/client`](src/SproutForms.Client) is a framework-agnostic client for the API, written in TypeScript. It doesn't render anything; it gives your components what they need:

```ts
import { createSproutFormsClient, validateForm, isFieldVisible, getNextPageIndex, handleOutcome, registerOutcomeHandler } from '@sproutforms/client';

const client = createSproutFormsClient({ baseUrl: 'https://cms.example.com' });
const form = await client.getDefinition('contact');

// Render form.pages[i].rows[j].columns[k].field, the same shape as the Razor view model.
// Hide a field while isFieldVisible(field, values) is false, and skip pages with getNextPageIndex(form, values, current).

const errors = await validateForm(form, values);            // the same rules and conditions as the server
if (Object.keys(errors).length === 0) {
    const result = await client.submit(form, { values });   // adds the guard's values; uses multipart when a value is a File
    if (!result.ok) showErrors(result.errors);
    else if (!await handleOutcome(result.outcome, { definition: form })) showMessage(form.texts.submitSucceeded);
}

registerOutcomeHandler('redirectUmbracoPage', outcome => router.push(String(outcome.data.path)));
```

It also has:
- `client.validatePage` for paged forms, and `client.revalidateDefinition` to check whether a copy you cached is out of date.
- `calculateVariables(form, values)`, which works out the variables the definition holds the way the server does. Pass them to `isFieldVisible` and `isFieldRequired` when a form's conditions use variables; the page functions and `validateForm` work them out themselves.
- `isFieldOfType(field, 'select')`, which types a built-in field's `configuration`.
- `registerValidator` for the validation rules of a custom field type. Rules without a validator are only checked by the server.
- `registerSubmissionGuard` for a custom guard. The honeypot and reCAPTCHA v3 are built in; call `loadSubmissionGuard(form)` when the form shows.

forms.js uses the same condition and validation code, so a Razor form and a headless form behave the same way.

## Trying it out

[`src/SproutForms.Client/example`](src/SproutForms.Client/example) is a playground for the headless API. It renders any form of the demo site with plain TypeScript, the way a front-end would (`src/form-renderer.ts`), and shows next to it:

- **Requests**: every call to the API, with its headers and bodies.
- **Definition**: what the definition endpoint returned.
- **Stored submission**: the form's newest submission and its workflows.
- **Raw request**: send any body to the API, such as a value the form can't produce.

Switches in its header skip the validation in the browser, so you see the server's, or fill in the honeypot. It can also send an API key.

Start the demo site and the playground together, which opens `http://localhost:5173`:

```
pwsh -File scripts/ai-test/run-headless-playground.ps1
```

`-Reset` starts from a clean database. The demo site's `AiTest` environment has the headless API on, with the playground's origin allowed.

## Showing settings to a headless front-end

A field type decides what the browser sees of its configuration with `GetClientConfiguration`. By default that's the whole configuration, so override it when yours holds something only the server needs:

```csharp
protected override object? GetClientConfiguration(FileFieldConfig configuration)
    => new FileFieldClientConfig { MaxFileSizeBytes = configuration.MaxFileSizeBytes, AllowedExtensions = configuration.AllowedExtensions };
```

A form type shows nothing of its settings or field extensions unless it overrides `GetClientSettings(definition)` or `GetClientFieldExtension(field)`. Razor views still get the whole configuration.

# Extending: form types

A **form type** decides what kind of form something is: a standard form, a quiz, a poll, a product finder, or your own. Editors choose it when they create a form, and it can't be changed afterwards. A form type can:

- allow only some field types and submit outcomes (a poll only has radio buttons),
- add settings to the whole form (a quiz's pass mark),
- add settings to every field of a field type (a quiz question's correct answer and points), shown on a tab of that field named after the form type,
- process each submission before it is saved, to store results with it (a score) or to reject it,
- come with outcomes that show those results to the visitor.

For a score, a personality type or a price, [calculations](#calculations) usually do without a form type or any code.

Working examples of a quiz, a poll and a product finder are in [`src/SproutForms.Site/Examples`](src/SproutForms.Site/Examples). Each one is a form type, a backoffice descriptor, an outcome, and a code-first example form; [`form-type-examples.js`](src/SproutForms.Site/wwwroot/examples/form-type-examples.js) holds their front-end handlers. The steps below build the quiz.

## 1. The form type

```csharp
public class QuizFormType : FormDefinitionTypeBase<QuizSettings>
{
    public override string Alias => "quiz";

    public QuizFormType()
    {
        // Every radio button field in a quiz gets these settings
        ExtendField<QuizAnswerSettings>("radio");
    }

    public override bool AllowsFieldType(IFormFieldType fieldType) => fieldType is not FileFieldType;

    // Runs after field validation, before the submission is saved
    public override Task<FormTypeSubmissionResult> ProcessSubmissionAsync(FormTypeSubmissionContext context, CancellationToken cancellationToken)
    {
        var settings = GetSettings(context.Definition);
        var score = 0;
        foreach (var field in context.Definition.Fields.Where(it => it.Extension != null))
        {
            var answer = GetFieldSettings<QuizAnswerSettings>(field);
            if (context.Values.TryGetValue(field.Alias, out var value) && value.GetString() == answer.CorrectAnswer)
                score += answer.Points;
        }

        // Stored with the submission as FormSubmission.Results.
        // Use FormTypeSubmissionResult.Reject(fieldAlias, message) to refuse the submission instead.
        return Task.FromResult(FormTypeSubmissionResult.WithResults(new Dictionary<string, object?>
        {
            ["score"] = score,
            ["passed"] = score >= settings.PassMark
        }));
    }
}

public class QuizSettings { public int PassMark { get; set; } }
public class QuizAnswerSettings { public string CorrectAnswer { get; set; } = ""; public int Points { get; set; } = 1; }
```

The settings classes are plain classes; they are saved with each version of the form.

## 2. The backoffice descriptor

The descriptor names the type in the "Create a form" dialog and maps its settings to property editors, the same way field, workflow and outcome descriptors do.

```csharp
public class QuizFormTypeDescriptor : BaseFormDefinitionTypeDescriptor<QuizSettings>
{
    public override string FormTypeAlias => "quiz";
    public override string DisplayName => "Quiz";
    public override string Description => "Questions with a correct answer and points.";

    public QuizFormTypeDescriptor()
    {
        DefineMap(it => it.PassMark, "passMark", "Pass mark", "Umb.PropertyEditorUi.Integer");

        ExtendField<QuizAnswerSettings>("radio", field => field
            // A dropdown of the question's own options
            .Map(it => it.CorrectAnswer, "correctAnswer", "Correct answer", "SproutForms.FieldOptionPicker")
            .Map(it => it.Points, "points", "Points", "Umb.PropertyEditorUi.Integer"));
    }
}
```

A type without a descriptor still works for code-first forms, but editors can't choose it.

## 3. An outcome that shows the result

An outcome gets the saved submission, including the results, and returns data for the browser. `url` redirects and `message` is shown, also without JavaScript; anything else is for your own front-end handler.

```csharp
public class QuizResultOutcomeType : IFormSubmitOutcomeType, IRestrictedToFormTypes
{
    public string Alias => "quizResult";
    public Type ConfigurationType => typeof(QuizResultOutcomeConfig);
    public object GetDefaultConfiguration() => new QuizResultOutcomeConfig();

    // Only quizzes have a score, so only quizzes can pick this outcome
    public IReadOnlyCollection<string> FormTypeAliases => ["quiz"];

    public Task<OutcomeResult> HandleAsync(FormSubmitOutcomeContext context, CancellationToken cancellationToken)
    {
        var score = context.Submission.Results["score"].GetInt32();
        return Task.FromResult(new OutcomeResult
        {
            Data = new Dictionary<string, object?> { ["score"] = score, ["message"] = $"You scored {score}." }
        });
    }
}
```

Give it a descriptor (`BaseOutcomeDescriptor<QuizResultOutcomeConfig>`) so editors can choose it. `message` is inserted as HTML, so never put submitted values in it.

Register a handler for the outcome's alias after `forms.js` is loaded:

```js
window.SproutForms.outcomeHandlers.register("quizResult", (form, data) => {
    const result = document.createElement("div");
    result.className = "form-success";
    result.textContent = `You scored ${data.score}`;
    form.replaceChildren(result);
});
```

## 4. Register it

```csharp
public class QuizComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddSingleton<IFormDefinitionType, QuizFormType>();
        builder.Services.AddSingleton<IFormDefinitionTypeDescriptor, QuizFormTypeDescriptor>();
        builder.Services.AddSingleton<IFormSubmitOutcomeType, QuizResultOutcomeType>();
        builder.Services.AddSingleton<IOutcomeDescriptor, QuizResultOutcomeDescriptor>();
    }
}
```

## 5. In code

Code-first forms choose the type and set each field's extension settings:

```csharp
new FormBuilder("coffeeQuiz", "Coffee quiz")
    .OfType("quiz", new QuizSettings { PassMark = 2 })
    .Row(row => row.Col(12, col => col
        .Radio("strongest", "Which has the most caffeine per ml?")
            .Set(c => c.Options = [new() { Label = "Espresso", Value = "espresso" }, new() { Label = "Filter", Value = "filter" }])
            .Extend(new QuizAnswerSettings { CorrectAnswer = "espresso", Points = 2 })
            .Done()))
    .SetOutcome("quizResult", new QuizResultOutcomeConfig())
    .Build();
```

`WorkflowBuilder.Add(alias, workflowTypeAlias, configuration)` adds your own workflow types the same way. Workflows get the submission too, so a workflow can email the score.

Forms are checked against their type when a code-first form is registered (an invalid form stops the site from starting, with the reason) and when an editor saves one.
