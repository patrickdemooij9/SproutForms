using SproutForms.Core.Models.Conditions;
using System;
using System.Collections.Generic;
using System.Text;

namespace SproutForms.Core.Builders
{
    public class ConditionBuilder
    {
        private readonly List<ConditionRule> _rules = new();

        public ConditionBuilder Field(string fieldAlias, ConditionComparison comparison, object value)
        {
            _rules.Add(new ConditionRule
            {
                FieldAlias = fieldAlias,
                Comparison = comparison,
                Value = value
            });
            return this;
        }

        public ConditionDefinition Build() => new()
        {
            Operator = "All",
            Rules = _rules
        };
    }
}
