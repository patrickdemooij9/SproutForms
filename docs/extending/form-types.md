# Form types

A **form type** decides what kind of form something is: a standard form, a quiz, a poll, a product finder, or your own. Editors choose it when they create a form, and it can't be changed afterwards. A form type can:

- allow only some field types and submit outcomes (a poll only has radio buttons),
- add settings to the whole form (a quiz's pass mark),
- add settings to every field of a field type (a quiz question's correct answer and points), shown on a tab of that field named after the form type,
- process each submission before it is saved, to store results with it (a score) or to reject it,
- come with outcomes that show those results to the visitor.

For a score, a personality type or a price, [calculations](../forms/calculations.md) usually do without a form type or any code.

Working examples of a quiz, a poll and a product finder are in [`src/SproutForms.Site/Examples`](../../src/SproutForms.Site/Examples). Each one is a form type, a backoffice descriptor, an outcome, and a code-first example form; [`form-type-examples.js`](../../src/SproutForms.Site/wwwroot/examples/form-type-examples.js) holds their front-end handlers. The steps below build the quiz.

## 1. The form type

```csharp
using SproutForms.Core.Fields;
using SproutForms.Core.Models;
using SproutForms.Core.Models.FormTypes;

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

`FormDefinitionTypeBase` also has:

| Member | |
|---|---|
| `AllowsOutcomeType(outcomeType)` | Excludes outcome types, as `AllowsFieldType` does field types. |
| `GetDefaultSettings()` | The settings a new form of this type starts with; a new `TSettings` by default. |
| `GetClientSettings(definition)`, `GetClientFieldExtension(field)` | What the browser may see of the settings; nothing by default. See [Showing settings to the browser](../extending-headless/client-settings.md). |

`context` holds the submitted `Values`, the `Variables` the form's [calculations](../forms/calculations.md) worked out, the `Version` and its `Definition`. A rejection's key is a field alias, and the error shows on that field; any other key shows at the top of the form.

## 2. The backoffice descriptor

The descriptor names the type in the "Create a form" dialog and maps its settings to property editors, the same way field, workflow and outcome descriptors do.

```csharp
using SproutForms.Umbraco.Core.Descriptors.FormTypes;

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

A type without a descriptor still works for code-first forms, but editors can't choose it. Create only asks for the form type when more than one type has a descriptor.

`SproutForms.FieldOptionPicker` is a dropdown of the edited field's own options, for field types that keep them under `options`, like radio buttons and dropdowns. Custom field editors are `formFieldConfig` backoffice extensions, and receive the field being edited as `formField`.

## 3. An outcome that shows the result

An outcome gets the saved submission, including the results, and returns data for the browser. `url` redirects and `message` is shown when the visitor has no JavaScript; anything else is for your own front-end handler. See [Custom outcome types](outcome-types.md).

```csharp
using SproutForms.Core.Models.FormTypes;
using SproutForms.Core.Models.Outcomes;
using SproutForms.Umbraco.Core.Descriptors.Outcomes;

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

public class QuizResultOutcomeConfig { }

public class QuizResultOutcomeDescriptor : BaseOutcomeDescriptor<QuizResultOutcomeConfig>
{
    public override string OutcomeTypeAlias => "quizResult";
    public override string DisplayName => "Show quiz score";
    public override string Description => "Shows the visitor their score.";
}
```

`message` is inserted as HTML, so never put submitted values in it.

Register a handler for the outcome's alias after `forms.js` is loaded:

```js
window.SproutForms.registerOutcomeHandler("quizResult", (outcome, { form }) => {
    const result = document.createElement("div");
    result.className = "form-success";
    result.textContent = `You scored ${outcome.data.score}`;
    form.replaceChildren(result);
});
```

For a headless front-end, see [Custom outcome handlers for headless](../extending-headless/outcome-handlers.md).

## 4. Register it

```csharp
using Microsoft.Extensions.DependencyInjection;
using SproutForms.Core.Models.FormTypes;
using SproutForms.Core.Models.Outcomes;
using SproutForms.Umbraco.Core.Descriptors.FormTypes;
using SproutForms.Umbraco.Core.Descriptors.Outcomes;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

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

Without Umbraco, register the form type and the outcome type in `Program.cs`, without the descriptors.

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
