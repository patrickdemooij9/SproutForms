using SproutForms.Core.Builders;
using SproutForms.Core.Models;

namespace SproutForms.Site.Code
{
    /// <summary>
    /// Regression form for the verify-in-site flow: one required and one optional checkbox.
    /// </summary>
    public class AiTestRequiredCheckboxForm : ICodeFirstForm
    {
        public string Alias => "aiTestRequiredCheckbox";

        public FormDefinition Build()
        {
            return new FormBuilder("aiTestRequiredCheckbox", "AI test: required checkbox")
                .Row(row => row
                    .Col(12, col => col
                        .Text("name", "Name")
                            .Required()
                            .Done()
                    )
                )
                .Row(row => row
                    .Col(12, col => col
                        .Checkbox("mustAgree", "Required checkbox")
                            .Required()
                            .Done()
                    )
                )
                .Row(row => row
                    .Col(12, col => col
                        .Checkbox("newsletter", "Optional checkbox")
                            .Done()
                    )
                )
                .ThankYouMessage("Thank you!")
                .Build();
        }
    }
}
