using SproutForms.Core.Builders;
using SproutForms.Core.Fields.Configs;
using SproutForms.Core.Models;

namespace SproutForms.Site.Code
{
    /// <summary>
    /// Regression form for the verify-in-site flow: it uses a form type other than standard, with form settings and a field extension.
    /// </summary>
    public class AiTestFormTypeForm : ICodeFirstForm
    {
        public string Alias => "aiTestFormType";

        public FormDefinition Build()
        {
            return new FormBuilder("aiTestFormType", "AI test: form type")
                .OfType(AiTestQuizFormType.TypeAlias, new AiTestQuizSettings { PassMark = 2 })
                .Row(row => row
                    .Col(12, col => col
                        .Radio("coffee", "Which coffee has the most caffeine per ml?")
                            .Set(c => c.Options =
                            [
                                new RadioFieldOption { Label = "Espresso", Value = "espresso" },
                                new RadioFieldOption { Label = "Filter coffee", Value = "filter" }
                            ])
                            .Extend(new AiTestQuizAnswerSettings { CorrectAnswer = "espresso", Points = 2 })
                            .Required()
                            .Done()
                    )
                )
                .ThankYouMessage("Thanks for taking the quiz!")
                .Build();
        }
    }
}
