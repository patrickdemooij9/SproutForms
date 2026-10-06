using SproutForms.Core.Models.Calculations;
using SproutForms.Core.Models.Conditions;

namespace SproutForms.Core.Builders
{
    /// <summary>
    /// Builds a condition: all its rules must hold, or any of them after <see cref="Any"/>. A rule reads a field or a variable, and
    /// compares it with a value, or with another field or variable through <see cref="ValueOf"/>.
    /// </summary>
    public class ConditionBuilder
    {
        private readonly List<ConditionRule> _rules = new();
        private bool _any;

        public ConditionBuilder Field(string fieldAlias, ConditionComparison comparison, object value)
            => new ConditionRuleBuilder(this, fieldAlias, variableAlias: null).Compare(comparison, value);

        public ConditionRuleBuilder Field(string fieldAlias) => new(this, fieldAlias, variableAlias: null);

        public ConditionRuleBuilder Variable(string variableAlias) => new(this, string.Empty, variableAlias);

        /// <summary>
        /// The condition holds when any of its rules does, instead of all of them.
        /// </summary>
        public ConditionBuilder Any()
        {
            _any = true;
            return this;
        }

        internal ConditionBuilder Add(ConditionRule rule)
        {
            _rules.Add(rule);
            return this;
        }

        public ConditionDefinition Build() => new()
        {
            Operator = _any ? "Any" : "All",
            Rules = _rules
        };

        internal static ConditionDefinition Build(Action<ConditionBuilder> configure)
        {
            var builder = new ConditionBuilder();
            configure(builder);
            return builder.Build();
        }
    }

    /// <summary>
    /// One rule of a condition, on the field or variable it reads.
    /// </summary>
    public class ConditionRuleBuilder
    {
        private readonly ConditionBuilder _condition;
        private readonly string _fieldAlias;
        private readonly string? _variableAlias;

        internal ConditionRuleBuilder(ConditionBuilder condition, string fieldAlias, string? variableAlias)
        {
            _condition = condition;
            _fieldAlias = fieldAlias;
            _variableAlias = variableAlias;
        }

        public ConditionBuilder Is(object value) => Compare(ConditionComparison.Equals, value);
        public ConditionBuilder IsNot(object value) => Compare(ConditionComparison.NotEquals, value);
        public ConditionBuilder Contains(object value) => Compare(ConditionComparison.Contains, value);
        public ConditionBuilder GreaterThan(object value) => Compare(ConditionComparison.GreaterThan, value);
        public ConditionBuilder LessThan(object value) => Compare(ConditionComparison.LessThan, value);
        public ConditionBuilder IsEmpty() => Compare(ConditionComparison.IsEmpty, null);
        public ConditionBuilder IsNotEmpty() => Compare(ConditionComparison.IsNotEmpty, null);
        public ConditionBuilder Matches(string pattern) => Compare(ConditionComparison.MatchesRegex, pattern);
        public ConditionBuilder DoesNotMatch(string pattern) => Compare(ConditionComparison.DoesNotMatchRegex, pattern);

        /// <param name="value">A value, or another field or variable from <see cref="ValueOf"/></param>
        public ConditionBuilder Compare(ConditionComparison comparison, object? value)
            => _condition.Add(new ConditionRule
            {
                FieldAlias = _fieldAlias,
                VariableAlias = _variableAlias,
                Comparison = comparison,
                // A value from ValueOf names the field or variable to compare with
                Value = value is CalculationOperand reference ? reference.Value : value,
                ValueSource = value is CalculationOperand operand ? operand.Source : ConditionValueSource.Value
            });
    }
}
