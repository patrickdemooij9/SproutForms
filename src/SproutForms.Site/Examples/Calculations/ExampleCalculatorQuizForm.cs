using SproutForms.Core.Builders;
using SproutForms.Core.Fields.Configs;
using SproutForms.Core.Models;
using SproutForms.Core.Models.Outcomes;

namespace SproutForms.Site.Examples.Calculations
{
    /// <summary>
    /// The coffee quiz again, as a standard form: a score variable counts the points, and the outcome depends on it. No form type or
    /// custom code is involved, so an editor can build the same in the backoffice's Calculations tab.
    /// </summary>
    public class ExampleCalculatorQuizForm : ICodeFirstForm
    {
        public string Alias => "exampleCalculatorQuiz";

        public FormDefinition Build()
        {
            return new FormBuilder(Alias, "Example: coffee quiz with calculations")
                .Row(row => row
                    .Col(12, col => col
                        .Radio("strongest", "Which has the most caffeine per ml?")
                            .Set(c => c.Options =
                            [
                                new RadioFieldOption { Label = "Espresso", Value = "espresso" },
                                new RadioFieldOption { Label = "Filter coffee", Value = "filter" }
                            ])
                            .Required()
                            .Done()
                    )
                )
                .Row(row => row
                    .Col(12, col => col
                        .Select("origin", "Where was coffee first brewed?")
                            .Set(c => c.Options =
                            [
                                new SelectFieldOption { Label = "Brazil", Value = "brazil" },
                                new SelectFieldOption { Label = "Yemen", Value = "yemen" },
                                new SelectFieldOption { Label = "Italy", Value = "italy" }
                            ])
                            .Required()
                            .Done()
                    )
                )
                // Not exposed, so the browser never sees the answers
                .Variable("score", v => v.Label("Score"))
                .Calculate("score", rules => rules
                    .When(c => c.Field("strongest").Is("espresso")).Add(2)
                    .When(c => c.Field("origin").Is("yemen")).Add(1))
                .SetOutcome(ShowMessageOutcome.Alias, new ShowMessageOutcomeConfig { Message = "You scored {var:score} of 3. Time for another cup and a second try." })
                .SetOutcomeWhen(c => c.Variable("score").Is(3), ShowMessageOutcome.Alias, new ShowMessageOutcomeConfig { Message = "A perfect {var:score} of 3. You know your coffee!" })
                .Build();
        }
    }
}
