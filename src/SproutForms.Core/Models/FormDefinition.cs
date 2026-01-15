using SproutForms.Core.Models.Flows;
using SproutForms.Core.Models.Outcomes;

namespace SproutForms.Core.Models
{
    public class FormDefinition
    {
        public List<FormRow> Rows { get; set; } = [];
        public List<FormField> Fields { get; set; } = [];
        public List<FormWorkflow> Workflows { get; set; } = [];

        public FormSubmitOutcome SubmitOutcome { get; set; } = FormSubmitOutcome.Default();
    }
}
