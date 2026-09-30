using SproutForms.Core.Builders;
using SproutForms.Core.Models;
using SproutForms.Core.Models.Conditions;

namespace SproutForms.Site.Code
{
    /// <summary>
    /// Regression form for the verify-in-site flow: a repeater of attendees (1 to 3, two shown at first) with a required name, an optional email,
    /// an allergies checkbox that shows and requires the allergies field of the same entry, and an upload per entry.
    /// </summary>
    public class AiTestRepeaterForm : ICodeFirstForm
    {
        public string Alias => "aiTestRepeater";

        public FormDefinition Build()
        {
            var definition = new FormBuilder("aiTestRepeater", "AI test: repeater")
                .Row(row => row
                    .Col(12, col => col.Text("team", "Team name").Required().Done())
                )
                .Row(row => row
                    .Col(12, col => col
                        .Repeater("attendees", "Attendees", group => group
                            .Row(entry => entry
                                .Col(6, child => child.Text("attendeeName", "Name").Required().Done())
                                .Col(6, child => child.Email("attendeeEmail", "Email").Done())
                            )
                            .Row(entry => entry
                                .Col(6, child => child.Checkbox("hasAllergies", "Has allergies").Done())
                                .Col(6, child => child.Text("allergies", "Allergies").Done())
                            )
                            .Row(entry => entry
                                .Col(12, child => child.File("ticket", "Ticket").Done())
                            )
                        )
                        .Set(c =>
                        {
                            c.MinItems = 1;
                            c.MaxItems = 3;
                            c.InitialItems = 2;
                            c.ItemTitle = "Attendee {n}";
                            c.AddLabel = "Add attendee";
                        })
                        .Done()
                    )
                )
                .ThankYouMessage("Thank you!")
                .OnSubmit(c => c.SendEmail("email", config =>
                    config
                        .To("events@sproutforms.local")
                        .From("noreply@sproutforms.local")
                        .Subject("Repeater form submitted"))
                )
                .Build();

            var hasAllergies = new ConditionDefinition
            {
                Rules = [new ConditionRule { FieldAlias = "hasAllergies", Comparison = ConditionComparison.Equals, Value = "true" }]
            };
            definition.FindField("allergies")!.Conditions = new FieldConditions { Visibility = hasAllergies, Required = hasAllergies };

            return definition;
        }
    }
}
