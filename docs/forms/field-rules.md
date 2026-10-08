# Field rules

A field's rules say when it shows, hides or is required: each rule has a condition and an action. A field is hidden while any of its Hide rules holds and, when it has Show rules, only shown while one of them holds. It is required while any of its Require rules holds. A hidden field isn't validated, in the browser or on the server, and its value isn't stored. A rule can also change a variable, see [Calculations](calculations.md).

The browser and the server check the same rules: forms.js and the headless packages run the same conditions as the server, so a field shows and hides as the visitor fills in the form.

## In the backoffice

A field's rules are on its Rules tab.

## In code

`VisibleWhen`, `HiddenWhen` and `RequiredWhen` each add a rule:

```csharp
.Col(12, col => col.Text("address", "Address")
    .VisibleWhen(c => c.Field("delivery").Is("home"))
    .RequiredWhen(c => c.Field("delivery").Is("home"))
    .Done())
```

## Conditions

A condition has one or more rules. All of them must hold, or any of them after `Any()`:

```csharp
.VisibleWhen(c => c.Any()
    .Field("country").Is("NL")
    .Field("country").Is("BE"))
```

A rule reads a field (`c.Field(alias)`) or a [variable](calculations.md) (`c.Variable(alias)`), and compares it:

| Method | `ConditionComparison` |
|---|---|
| `Is(value)` | `Equals` |
| `IsNot(value)` | `NotEquals` |
| `Contains(value)` | `Contains` |
| `GreaterThan(value)` | `GreaterThan` |
| `LessThan(value)` | `LessThan` |
| `IsEmpty()` | `IsEmpty` |
| `IsNotEmpty()` | `IsNotEmpty` |
| `Matches(pattern)` | `MatchesRegex` |
| `DoesNotMatch(pattern)` | `DoesNotMatchRegex` |

`c.Field(alias, comparison, value)` is the same as `c.Field(alias).Compare(comparison, value)`. `ConditionComparison` is in `SproutForms.Core.Models.Conditions`.

A value is compared as written, or with another field or variable through `ValueOf`:

```csharp
.VisibleWhen(c => c.Field("confirmEmail").IsNot(ValueOf.Field("email")))
```

A field's condition may use fields on earlier pages and on its own page, but not on later ones, and a field's visibility can't depend on the field itself. Fields inside a [repeater](built-in-types.md#field-types) can use the other fields of their entry and the fields their repeater may use; nothing outside a repeater can use the fields inside it.

The same conditions are used by [page conditions](pages.md#pages-that-depend-on-answers), calculation rules and conditional outcomes.
