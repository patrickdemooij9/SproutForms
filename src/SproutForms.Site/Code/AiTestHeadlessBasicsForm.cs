using SproutForms.Core.Builders;
using SproutForms.Core.Fields.Configs;
using SproutForms.Core.Models;
using SproutForms.Core.Models.Conditions;

namespace SproutForms.Site.Code
{
    /// <summary>
    /// Form for the Vue and Nuxt playgrounds: every field type the renderers cover so far on one page, with a field that only shows,
    /// and is only required, for one topic.
    /// </summary>
    public class AiTestHeadlessBasicsForm : ICodeFirstForm
    {
        public string Alias => "aiTestHeadlessBasics";

        public FormDefinition Build()
        {
            return new FormBuilder("aiTestHeadlessBasics", "AI test: headless basics")
                .Row(row => row
                    .Col(6, col => col.Text("name", "Name").Required().Done())
                    .Col(6, col => col.Email("email", "Email").Required().Done())
                )
                .Row(row => row
                    .Col(6, col => col
                        .Select("topic", "Topic")
                            .Required()
                            .Set(c => c.Options =
                            [
                                new SelectFieldOption { Label = "A question", Value = "question" },
                                new SelectFieldOption { Label = "A quote", Value = "quote" },
                                new SelectFieldOption { Label = "Something else", Value = "other" }
                            ])
                            .Done()
                    )
                    .Col(6, col => col
                        .Text("company", "Company")
                            .VisibleWhen(c => c.Field("topic", ConditionComparison.Equals, "quote"))
                            .RequiredWhen(c => c.Field("topic", ConditionComparison.Equals, "quote"))
                            .Done()
                    )
                )
                .Row(row => row
                    .Col(12, col => col.Textarea("message", "Message").Required().Done())
                )
                .Row(row => row
                    .Col(12, col => col.Checkbox("newsletter", "Send me the newsletter").Done())
                )
                .Row(row => row
                    .Col(12, col => col
                        .Hidden("source", "Source")
                            .Set(c => c.DefaultValue = "headless")
                            .Done()
                    )
                )
                .SubmitLabel("Send")
                .ThankYouMessage("<p><strong>Thank you!</strong> We'll get back to you soon.</p>")
                .Build();
        }
    }
}
