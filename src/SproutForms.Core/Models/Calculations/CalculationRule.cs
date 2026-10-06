using SproutForms.Core.Models.Conditions;

namespace SproutForms.Core.Models.Calculations
{
    /// <summary>
    /// One step of a form's calculations: when its condition holds, it changes a variable. A form's rules run top to bottom, so a rule
    /// sees what the rules above it did.
    /// </summary>
    public class CalculationRule
    {
        // No condition, or one without rules, always holds
        public ConditionDefinition? Condition { get; set; }

        public required string VariableAlias { get; set; }
        public CalculationOperation Operation { get; set; } = CalculationOperation.Add;
        public CalculationOperand Operand { get; set; } = new();

        // The field the backoffice lists the rule on, among that field's rules; it still runs in its place in the form's list
        public string? OwnerFieldAlias { get; set; }
    }
}
