using SproutForms.Core.Models.Conditions;

namespace SproutForms.Core.Models.Calculations
{
    /// <summary>
    /// The value a calculation rule works with: a value as written, or the value of a field or a variable.
    /// </summary>
    public class CalculationOperand
    {
        public ConditionValueSource Source { get; set; } = ConditionValueSource.Value;

        // The value itself, or the alias of the field or variable
        public object? Value { get; set; }
    }
}
