using SproutForms.Core.Builders;
using SproutForms.Core.Fields.Configs;
using SproutForms.Core.Models;

namespace SproutForms.Site.Examples.Quiz
{
    /// <summary>
    /// A quiz built in code. The same can be built in the backoffice: Create → Quiz.
    /// </summary>
    public class ExampleQuizForm : ICodeFirstForm
    {
        public string Alias => "exampleQuiz";

        public FormDefinition Build()
        {
            return new FormBuilder(Alias, "Example: coffee quiz")
                .OfType(QuizFormType.TypeAlias, new QuizSettings
                {
                    PassMark = 3,
                    PassedMessage = "You know your coffee!",
                    FailedMessage = "Time for another cup and a second try."
                })
                .Row(row => row
                    .Col(12, col => col
                        .Radio("strongest", "Which has the most caffeine per ml?")
                            .Set(c => c.Options =
                            [
                                new RadioFieldOption { Label = "Espresso", Value = "espresso" },
                                new RadioFieldOption { Label = "Filter coffee", Value = "filter" }
                            ])
                            .Extend(new QuizAnswerSettings { CorrectAnswer = "espresso", Points = 2 })
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
                            .Extend(new QuizAnswerSettings { CorrectAnswer = "yemen", Points = 1 })
                            .Required()
                            .Done()
                    )
                )
                .SetOutcome(QuizResultOutcomeType.TypeAlias, new QuizResultOutcomeConfig())
                .Build();
        }
    }
}
