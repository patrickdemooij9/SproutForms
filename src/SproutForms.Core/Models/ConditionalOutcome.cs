using SproutForms.Core.Models.Conditions;

namespace SproutForms.Core.Models
{
    /// <summary>
    /// A submit outcome that replaces the form's own when its condition holds, such as a result page per score.
    /// </summary>
    public class ConditionalOutcome
    {
        public ConditionDefinition Condition { get; set; } = new();
        public required FormSubmitOutcome Outcome { get; set; }
    }
}
