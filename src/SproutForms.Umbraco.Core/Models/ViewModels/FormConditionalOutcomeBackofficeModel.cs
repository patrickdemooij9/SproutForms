using SproutForms.Core.Models.Conditions;

namespace SproutForms.Umbraco.Core.Models.ViewModels
{
    public class FormConditionalOutcomeBackofficeModel
    {
        public ConditionDefinition Condition { get; set; } = new();
        public required FormOutcomeBackofficeModel Outcome { get; set; }
    }
}
