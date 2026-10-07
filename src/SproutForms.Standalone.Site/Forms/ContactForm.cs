using SproutForms.Core.Builders;
using SproutForms.Core.Models;

namespace SproutForms.Standalone.Site.Forms
{
    public class ContactForm : ICodeFirstForm
    {
        public string Alias => "contact";

        public FormDefinition Build()
        {
            return new FormBuilder("contact", "Contact")
                .Row(row => row
                    .Col(6, col => col.Text("name", "Name").Required().Done())
                    .Col(6, col => col.Email("email", "Email").Required().Done()))
                .Row(row => row
                    .Col(12, col => col.Textarea("message", "Message").Required().Done()))
                .OnSubmit(workflows => workflows.SendEmail("notify", email => email
                    .To("info@example.com")
                    .From("noreply@example.com")
                    .Subject("New contact form submission")))
                .Build();
        }
    }
}
