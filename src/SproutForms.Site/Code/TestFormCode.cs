using SproutForms.Core.Builders;
using SproutForms.Core.Fields;
using SproutForms.Core.Fields.Configs;
using SproutForms.Core.Models;

namespace SproutForms.Site.Code
{
    public class TestFormCode : ICodeFirstForm
    {
        public string Alias => "testFormCode";

        public FormDefinition Build()
        {
            return new FormBuilder("test", "Test")
                .Row(row => row
                    .Col(6, col => col
                        .Text("firstName", "First name")
                            .Required()
                            .Done()
                    )
                    .Col(6, col => col
                        .Text("lastName", "Last name")
                            .Required()
                            .Done()
                    )
                )
                .Row(row => row
                    .Col(12, col => col
                        .Email("email", "Emailadress")
                        .Done()
                    )
                )
                .Row(row => row
                    .Col(12, col => col
                        .Textarea("message", "Your message")
                        .Done()
                    )
                )
                .Row(row => row
                    .Col(12, col => col
                        .Checkbox("agreeTermsAndConditions", "Do you agree with the terms and conditions?")
                        .Done()
                    )
                )
                .Row(row => row
                    .Col(12, col => col
                        .Select("favoFood", "What is your favorite food")
                        .Set(conf => conf.Options =
                        [
                            new SelectFieldOption { Label = "Pizza", Value = "pizza" },
                            new SelectFieldOption { Label = "Pasta", Value = "pasta" },
                            new SelectFieldOption { Label = "Salad", Value = "salad" },
                        ])
                        .Done()
                    )
                )
                .Row(row => row
                    .Col(12, col => col
                        .Hidden("spooky", "No label here")
                        .Done()
                    )
                )
                .Row(row => row
                    .Col(12, col => col
                        .Radio("bestBoardGame", "What is the best board game?")
                        .Set(c => c.Options =
                        [
                            new RadioFieldOption { Label = "Carcasonne", Value = "carcasonne" },
                            new RadioFieldOption { Label = "Ticket to ride", Value = "ticketToRide" }
                        ])
                        .Done()
                    )
                )
                .Row(row => row
                    .Col(6, col => col
                        .Date("fromDate", "From date")
                        .Done()
                    )
                    .Col(6, col => col
                        .Date("toDate", "To date")
                        .Done()
                    )
                )
                .ThankYouMessage("Thank you!")
                .OnSubmit(c => c.SendEmail("email", config =>
                    config
                        .To("patrickdemooij98@hotmail.com")
                        .From("info@skytearhordedb.com")
                        .Subject("Form has been filled in"))
                )
                .Build();
        }
    }
}
