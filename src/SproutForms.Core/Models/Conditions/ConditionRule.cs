namespace SproutForms.Core.Models.Conditions
{
    public class ConditionRule
    {
        public required string FieldAlias { get; init; }
        public ConditionComparison Comparison { get; init; }
        public object? Value { get; init; } // target value for comparison
    }
}
