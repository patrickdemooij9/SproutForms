using SproutForms.Core.Builders;
using SproutForms.Core.Models;

namespace SproutForms.Site.Code
{
    /// <summary>
    /// Regression form for the verify-in-site flow: three workflows where the middle one always fails,
    /// so the third must stay Pending behind it.
    /// </summary>
    public class AiTestWorkflowOrderForm : ICodeFirstForm
    {
        public string Alias => "aiTestWorkflowOrder";

        public FormDefinition Build()
        {
            return new FormBuilder("aiTestWorkflowOrder", "AI test: workflow order")
                .Row(row => row
                    .Col(12, col => col
                        .Text("name", "Name")
                            .Done()
                    )
                )
                .ThankYouMessage("Thank you!")
                .OnSubmit(s => s
                    .SendEmail("first", email => email
                        .To("first@sproutforms.local")
                        .From("noreply@sproutforms.local")
                        .Subject("First workflow"))
                    .PostToCustomEndpoint("second", post => post
                        .Url("http://127.0.0.1:9/"))
                    .SendEmail("third", email => email
                        .To("third@sproutforms.local")
                        .From("noreply@sproutforms.local")
                        .Subject("Third workflow")))
                .Build();
        }
    }
}
