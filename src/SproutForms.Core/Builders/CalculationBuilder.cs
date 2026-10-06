using SproutForms.Core.Models.Calculations;
using SproutForms.Core.Models.Conditions;

namespace SproutForms.Core.Builders
{
    /// <summary>
    /// Adds calculation rules for one variable, in order: <c>rules.When(c => c.Field("capital").Is("Paris")).Add(10)</c>.
    /// </summary>
    public class CalculationBuilder
    {
        private readonly string _variableAlias;
        private readonly List<CalculationRule> _rules;

        internal CalculationBuilder(string variableAlias, List<CalculationRule> rules)
        {
            _variableAlias = variableAlias;
            _rules = rules;
        }

        public CalculationStepBuilder When(Action<ConditionBuilder> condition) => new(this, ConditionBuilder.Build(condition));

        public CalculationStepBuilder Always() => new(this, null);

        internal CalculationBuilder Add(ConditionDefinition? condition, CalculationOperation operation, object? value)
        {
            _rules.Add(new CalculationRule
            {
                Condition = condition,
                VariableAlias = _variableAlias,
                Operation = operation,
                Operand = value as CalculationOperand ?? new CalculationOperand { Value = value }
            });
            return this;
        }
    }

    /// <summary>
    /// What a rule does when its condition holds. A value is used as written, or is another field or variable from <see cref="ValueOf"/>.
    /// </summary>
    public class CalculationStepBuilder
    {
        private readonly CalculationBuilder _calculation;
        private readonly ConditionDefinition? _condition;

        internal CalculationStepBuilder(CalculationBuilder calculation, ConditionDefinition? condition)
        {
            _calculation = calculation;
            _condition = condition;
        }

        public CalculationBuilder Set(object value) => _calculation.Add(_condition, CalculationOperation.Set, value);
        public CalculationBuilder Add(object value) => _calculation.Add(_condition, CalculationOperation.Add, value);
        public CalculationBuilder Subtract(object value) => _calculation.Add(_condition, CalculationOperation.Subtract, value);
        public CalculationBuilder Multiply(object value) => _calculation.Add(_condition, CalculationOperation.Multiply, value);
        public CalculationBuilder Divide(object value) => _calculation.Add(_condition, CalculationOperation.Divide, value);
        public CalculationBuilder Append(object value) => _calculation.Add(_condition, CalculationOperation.Append, value);
    }
}
