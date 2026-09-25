using SproutForms.Core.Models.Conditions;

namespace SproutForms.Core.Models
{
    public class FormPage
    {
        public string? Title { get; set; }
        public List<FormRow> Rows { get; set; } = [];

        // Empty labels fall back to the defaults when the form is rendered
        public string? NextLabel { get; set; }
        public string? PreviousLabel { get; set; }

        // A page without visibility conditions is always shown
        public ConditionDefinition? Visibility { get; set; }
    }
}
