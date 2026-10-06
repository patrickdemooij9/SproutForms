using SproutForms.Core.Builders;
using SproutForms.Core.Fields.Configs;
using SproutForms.Core.Models;
using SproutForms.Core.Models.Outcomes;

namespace SproutForms.Site.Examples.Calculations
{
    /// <summary>
    /// Which coffee suits you: each answer adds to a counter, the highest counter wins, and a tie-breaker page only shows when the
    /// counters are level after the first page. The counters go to the browser, since that page's condition uses them; the result
    /// stays on the server.
    /// </summary>
    public class ExamplePersonalityTestForm : ICodeFirstForm
    {
        public string Alias => "examplePersonalityTest";

        public FormDefinition Build()
        {
            return new FormBuilder(Alias, "Example: which coffee are you?")
                .Page("Your mornings", page => page
                    .Row(row => row.Col(12, col => col
                        .Radio("morning", "How do you start your day?")
                            .Set(c => c.Options =
                            [
                                new RadioFieldOption { Label = "Quick, on the way out", Value = "quick" },
                                new RadioFieldOption { Label = "Slowly, with the paper", Value = "slow" }
                            ])
                            .Required()
                            .Done()))
                    .Row(row => row.Col(12, col => col
                        .Radio("taste", "What do you like best?")
                            .Set(c => c.Options =
                            [
                                new RadioFieldOption { Label = "Bold and short", Value = "bold" },
                                new RadioFieldOption { Label = "Smooth and milky", Value = "smooth" }
                            ])
                            .Required()
                            .Done())))
                .Page("Tie-breaker", page => page
                    .VisibleWhen(c => c.Variable("espresso").Is(ValueOf.Variable("latte")))
                    .Row(row => row.Col(12, col => col
                        .Radio("tieBreaker", "Pick a café")
                            .Set(c => c.Options =
                            [
                                new RadioFieldOption { Label = "A standing bar in Rome", Value = "rome" },
                                new RadioFieldOption { Label = "A sofa in Seattle", Value = "seattle" }
                            ])
                            .Required()
                            .Done())))
                .Variable("espresso", v => v.Label("Espresso points"))
                .Variable("latte", v => v.Label("Latte points"))
                .Variable("coffee", v => v.Label("Your coffee").Text().StartAt("Espresso"))
                .Calculate("espresso", rules => rules
                    .When(c => c.Field("morning").Is("quick")).Add(1)
                    .When(c => c.Field("taste").Is("bold")).Add(1))
                .Calculate("latte", rules => rules
                    .When(c => c.Field("morning").Is("slow")).Add(1)
                    .When(c => c.Field("taste").Is("smooth")).Add(1))
                // The tie-breaker can't add to the counters its own page depends on; it's empty unless its page showed
                .Calculate("coffee", rules => rules
                    .When(c => c.Variable("latte").GreaterThan(ValueOf.Variable("espresso"))).Set("Latte")
                    .When(c => c.Field("tieBreaker").Is("seattle")).Set("Latte"))
                .SetOutcome(ShowMessageOutcome.Alias, new ShowMessageOutcomeConfig { Message = "You're an {var:coffee}: short, strong and to the point." })
                .SetOutcomeWhen(c => c.Variable("coffee").Is("Latte"), ShowMessageOutcome.Alias, new ShowMessageOutcomeConfig { Message = "You're a {var:coffee}: take your time and enjoy it." })
                .Build();
        }
    }
}
