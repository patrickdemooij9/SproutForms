using SproutForms.Core.Models.Flows;
using SproutForms.Core.Models.FormTypes;
using SproutForms.Core.Models.Outcomes;

namespace SproutForms.Core.Models
{
    public class FormDefinition
    {
        // Definitions stored before form types existed have no type, and read as standard forms
        public FormDefinitionTypeReference Type { get; set; } = FormDefinitionTypeReference.Standard();

        public List<FormRow> Rows { get; set; } = [];
        public List<FormField> Fields { get; set; } = [];
        public List<FormWorkflow> Workflows { get; set; } = [];

        public FormSubmitOutcome SubmitOutcome { get; set; } = FormSubmitOutcome.Default();
    }
}
