# Calculations

A form can work out values from its answers, such as a quiz score, a personality type or a price, without any code. It has **variables**, each a number or text with a starting value, and **calculation rules** that change them. A rule has an optional condition and an operation: a number can be set, added to, subtracted from, multiplied or divided, and text can be set or appended to. The value a rule works with is a value you type, or the value of a field or another variable. The rules run top to bottom, so a rule sees what the rules above it did.

- A field the visitor doesn't see, because of its rules or a skipped page, counts as empty. A value that isn't a number counts as 0, and dividing by zero leaves a variable as it was.
- A number is rounded to its variable's decimals once all rules have run.
- Conditions can compare a field or a variable with a value, another field or another variable. For "the highest counter wins", compare the counters: `When introvert > extrovert, set personality to "Introvert"`.
- A calculation rule can be listed on a field, among the field's own rules ("when this answer is Paris, add 10 to score"). It still runs in its place in the form's list of rules.
- Page conditions and field rules can use variables too, to skip a page or show a field based on the answers so far. A variable a condition uses may only depend on earlier pages, and a field's visibility can't depend on the field itself; saving such a form fails with a message that says which variable and field.
- **Conditional outcomes** replace the form's outcome when their condition holds, such as a different message or result page per score range. They're checked top to bottom, and the first that holds is used; otherwise the form's own outcome is.
- `{var:alias}` fills in a variable's value in a message outcome (HTML-encoded), a redirect URL (URL-encoded), an email subject, and Slack and Teams messages (which also fill in `{alias}` with a field's answer). A number shows its variable's decimals. Emails list the variables under the answers, and a Custom POST sends them as `variables`.
- The server always works out the variables again when a form is submitted, whatever the browser sent, and stores them with the submission. The backoffice shows them with each submission, and as a column in the list.
- A variable stays on the server, with its rules, unless a page condition or field rule uses it. Then the browser gets it, and the rules it needs, to decide what to show. So a quiz score only reaches the browser when a condition uses it. A headless submit returns the values of the variables the browser got in `variables`.

## In the backoffice

Variables and all calculation rules are under Settings → Calculations, conditional outcomes under Settings → Outcomes, and a field's rules on its Rules tab.

## In code

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

`Variable` takes `Label(text)`, `Number(decimals)`, `Text()` and `StartAt(value)`; a variable is a number that starts at 0 unless you say otherwise. A rule starts with `When(condition)` or `Always()`, followed by `Set`, `Add`, `Subtract`, `Multiply`, `Divide` or `Append`. A rule's value can be `ValueOf.Field("quantity")` or `ValueOf.Variable("price")`, in conditions too: `c.Variable("latte").GreaterThan(ValueOf.Variable("espresso"))`.

```csharp
.Variable("total", v => v.Label("Total").Number(2))
.Calculate("total", rules => rules
    .Always().Set(ValueOf.Field("quantity"))
    .Always().Multiply(4.5))
```

The demo site has a calculated quiz, a personality test with a tie-breaker page, and a price quote in [`src/SproutForms.Site/Examples/Calculations`](../../src/SproutForms.Site/Examples/Calculations).

## In your own code

A form type's `ProcessSubmissionAsync` gets the variables in `context.Variables`, and an outcome finds them in `context.Submission.Variables`. See [Form types](../extending/form-types.md) and [Custom outcome types](../extending/outcome-types.md).

In a headless front-end, `calculateVariables(form, values)` from `@sproutforms/client` works out the variables the definition holds the way the server does; see [Install headless](../getting-started/headless/README.md#the-javascript-client).
