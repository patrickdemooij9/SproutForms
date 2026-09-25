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
3) Add the following scripts to your <head> tag:
```
<link rel="stylesheet" href="/forms/forms-layout.css" />
<link rel="stylesheet" href="/forms/forms-default-theme.css" />
<script src="/forms/forms.js"></script>
```
4) Add the following to your "_ViewImports.cshtml": `@addTagHelper *, SproutForms.Umbraco.Core`
5) You can now render your forms by using `<vc:render-form form-alias="testFileForm"></vc:render-form>` or `<vc:render-form form-id="[guid]"></vc:render-form>`

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

# Extending: form types

A **form type** decides what kind of form something is: a standard form, a quiz, a poll, a product finder, or your own. Editors choose it when they create a form, and it can't be changed afterwards. A form type can:

- allow only some field types and submit outcomes (a poll only has radio buttons),
- add settings to the whole form (a quiz's pass mark),
- add settings to every field of a field type (a quiz question's correct answer and points), shown on a tab of that field named after the form type,
- process each submission before it is saved, to store results with it (a score) or to reject it,
- come with outcomes that show those results to the visitor.

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
