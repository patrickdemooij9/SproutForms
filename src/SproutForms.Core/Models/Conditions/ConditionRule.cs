namespace SproutForms.Core.Models.Conditions
{
    public class ConditionRule
    {
        // The field the rule reads; empty when it reads a variable instead
        public string FieldAlias { get; init; } = string.Empty;

        // The variable the rule reads, when it doesn't read a field
        public string? VariableAlias { get; init; }

        public ConditionComparison Comparison { get; init; }

        // What the value is compared against: this value itself, or the alias of the field or variable whose value it is
        public object? Value { get; init; }
        public ConditionValueSource ValueSource { get; init; } = ConditionValueSource.Value;
    }
}
