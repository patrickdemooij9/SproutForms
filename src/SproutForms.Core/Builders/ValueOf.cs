using SproutForms.Core.Models.Calculations;
using SproutForms.Core.Models.Conditions;

namespace SproutForms.Core.Builders
{
    /// <summary>
    /// Refers to a field's or variable's value in a condition or calculation, where a plain value would be used as written.
    /// </summary>
    public static class ValueOf
    {
        public static CalculationOperand Field(string fieldAlias) => new() { Source = ConditionValueSource.Field, Value = fieldAlias };

        public static CalculationOperand Variable(string variableAlias) => new() { Source = ConditionValueSource.Variable, Value = variableAlias };
    }
}
