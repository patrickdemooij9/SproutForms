using SproutForms.Core.Builders;
using SproutForms.Core.Models;

namespace SproutForms.Site.Code
{
    /// <summary>
    /// Regression form for the verify-in-site flow: its submit outcome type isn't registered, as if it was removed
    /// after the form was made. A submit must still be saved and confirmed.
    /// </summary>
    public class AiTestUnknownOutcomeForm : ICodeFirstForm
    {
        public string Alias => "aiTestUnknownOutcome";

        public FormDefinition Build()
        {
            var definition = new FormBuilder("aiTestUnknownOutcome", "AI test: unknown outcome")
                .Row(row => row
                    .Col(12, col => col
                        .Text("name", "Name")
                            .Done()
                    )
                )
                .Build();

            // The builder only accepts registered outcome types
            definition.SubmitOutcome = new FormSubmitOutcome
            {
                OutcomeTypeAlias = "notRegistered",
                Configuration = new { }
            };
            return definition;
        }
    }
}
