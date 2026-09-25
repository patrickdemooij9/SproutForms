using SproutForms.Core.Builders;
using SproutForms.Core.Fields.Configs;
using SproutForms.Core.Models;
using SproutForms.Core.Models.Conditions;

namespace SproutForms.Site.Code
{
    /// <summary>
    /// Regression form for the verify-in-site flow: four pages with a required field on each, custom button labels, an upload on the second page and a third page that only shows for home delivery.
    /// </summary>
    public class AiTestMultiPageForm : ICodeFirstForm
    {
        public string Alias => "aiTestMultiPage";

        public FormDefinition Build()
        {
            return new FormBuilder("aiTestMultiPage", "AI test: multiple pages")
                .Page("About you", page => page
                    .NextLabel("Continue")
                    .Row(row => row
                        .Col(6, col => col.Text("name", "Name").Required().Done())
                        .Col(6, col => col.Email("email", "Email").Required().Done())
                    )
                    .Row(row => row
                        .Col(12, col => col
                            .Radio("delivery", "How do you want to receive your order?")
                                .Set(c => c.Options =
                                [
                                    new RadioFieldOption { Label = "Pick it up", Value = "pickup" },
                                    new RadioFieldOption { Label = "Home delivery", Value = "home" }
                                ])
                                .Done()
                        )
                    )
                )
                .Page("Your order", page => page
                    .PreviousLabel("Back")
                    .Row(row => row
                        .Col(12, col => col
                            .Text("orderCode", "Order code")
                                .Required()
                                .Set(c => c.Regex = "^[A-Z]{3}-[0-9]{3}$")
                                .Done()
                        )
                    )
                    .Row(row => row
                        .Col(12, col => col.File("receipt", "Receipt").Done())
                    )
                )
                .Page("Delivery address", page => page
                    .VisibleWhen(c => c.Field("delivery", ConditionComparison.Equals, "home"))
                    .Row(row => row
                        .Col(12, col => col.Text("address", "Address").Required().Done())
                    )
                )
                .Page(null, page => page
                    .Row(row => row
                        .Col(12, col => col.Textarea("message", "Message").Required().Done())
                    )
                )
                .SubmitLabel("Send order")
                .ThankYouMessage("Thank you!")
                .OnSubmit(c => c.SendEmail("email", config =>
                    config
                        .To("orders@sproutforms.local")
                        .From("noreply@sproutforms.local")
                        .Subject("Multi-page form submitted"))
                )
                .Build();
        }
    }
}
