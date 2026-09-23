using SproutForms.Core.Builders;
using SproutForms.Core.Models;

namespace SproutForms.Site.Code
{
    /// <summary>
    /// Regression form for the verify-in-site flow: its workflow posts to a port nothing listens on, so it always fails.
    /// </summary>
    public class AiTestFailingWorkflowForm : ICodeFirstForm
    {
        public string Alias => "aiTestFailingWorkflow";

        public FormDefinition Build()
        {
            return new FormBuilder("aiTestFailingWorkflow", "AI test: failing workflow")
                .Row(row => row
                    .Col(12, col => col
                        .Text("name", "Name")
                            .Done()
                    )
                )
                .ThankYouMessage("Thank you!")
                .OnSubmit(s => s
                    .PostToCustomEndpoint("unreachable", post => post
                        .Url("http://127.0.0.1:9/")))
                .Build();
        }
    }
}
