using SproutForms.Core.Models.Conditions;

namespace SproutForms.Core.Models.Calculations
{
    /// <summary>
    /// What the conditions and calculations of a form read: which fields and variables, and through them, which fields a variable's
    /// value can depend on.
    /// </summary>
    public sealed class CalculationDependencies
    {
        private readonly FormDefinition _definition;
        private readonly ILookup<string, CalculationRule> _rulesByVariable;

        public CalculationDependencies(FormDefinition definition)
        {
            _definition = definition;
            _rulesByVariable = definition.Calculations.ToLookup(rule => rule.VariableAlias);
        }

        public static IEnumerable<string> FieldsIn(ConditionDefinition? condition)
            => (condition?.Rules ?? []).SelectMany(FieldsIn);

        public static IEnumerable<string> VariablesIn(ConditionDefinition? condition)
            => (condition?.Rules ?? []).SelectMany(VariablesIn);

        public static IEnumerable<string> FieldsIn(ConditionRule rule)
        {
            if (string.IsNullOrEmpty(rule.VariableAlias) && !string.IsNullOrEmpty(rule.FieldAlias))
                yield return rule.FieldAlias;
            if (rule.ValueSource == ConditionValueSource.Field && Alias(rule.Value) is { } alias)
                yield return alias;
        }

        public static IEnumerable<string> VariablesIn(ConditionRule rule)
        {
            if (!string.IsNullOrEmpty(rule.VariableAlias))
                yield return rule.VariableAlias;
            if (rule.ValueSource == ConditionValueSource.Variable && Alias(rule.Value) is { } alias)
                yield return alias;
        }

        public static IEnumerable<string> FieldsIn(CalculationRule rule)
            => FieldsIn(rule.Condition).Concat(rule.Operand.Source == ConditionValueSource.Field && Alias(rule.Operand.Value) is { } alias ? [alias] : []);

        public static IEnumerable<string> VariablesIn(CalculationRule rule)
            => VariablesIn(rule.Condition).Concat(rule.Operand.Source == ConditionValueSource.Variable && Alias(rule.Operand.Value) is { } alias ? [alias] : []);

        /// <summary>
        /// The alias a value holds when it names a field or variable; null when it's empty.
        /// </summary>
        public static string? Alias(object? value)
            => VariableValues.ToText(value) is { Length: > 0 } alias ? alias : null;

        /// <summary>
        /// Every condition that decides whether a field is shown or required, or a page is gone through.
        /// </summary>
        public IEnumerable<ConditionDefinition> GetFormConditions()
            => _definition.GetAllFields()
                .SelectMany(field => field.Rules.Select(rule => rule.Condition))
                .Concat(_definition.Pages.Select(page => page.Visibility))
                .OfType<ConditionDefinition>();

        /// <summary>
        /// The conditions of a field's Show and Hide rules, which decide whether the visitor sees it.
        /// </summary>
        public static IEnumerable<ConditionDefinition> VisibilityConditions(FormField field)
            => field.Rules.Where(rule => rule.Action is FieldRuleAction.Show or FieldRuleAction.Hide).Select(rule => rule.Condition);

        /// <summary>
        /// True when a condition on a field or page reads a variable, so what's visible can change with the calculations.
        /// </summary>
        public bool FormConditionsUseVariables()
            => GetFormConditions().Any(condition => VariablesIn(condition).Any());

        /// <summary>
        /// The variables whose calculations the browser needs: those a field's or page's condition reads, and the variables their
        /// rules read in turn. The others never leave the server.
        /// </summary>
        public HashSet<string> GetClientVariables()
        {
            var aliases = _definition.Variables.Select(variable => variable.Alias).ToHashSet();
            var pending = new Stack<string>(GetFormConditions().SelectMany(VariablesIn));

            var result = new HashSet<string>();
            while (pending.TryPop(out var alias))
            {
                if (!aliases.Contains(alias) || !result.Add(alias))
                    continue;
                foreach (var dependency in _rulesByVariable[alias].SelectMany(VariablesIn))
                    pending.Push(dependency);
            }
            return result;
        }

        /// <summary>
        /// The fields a variable's value can depend on: the fields its rules read, those the variables they read depend on, and the
        /// fields that decide whether those fields are shown, since a hidden field counts as empty.
        /// </summary>
        public HashSet<string> GetFieldsBehind(string variableAlias)
        {
            var visitedVariables = new HashSet<string>();
            var fields = new HashSet<string>();

            void VisitVariable(string alias)
            {
                if (!visitedVariables.Add(alias))
                    return;
                foreach (var rule in _rulesByVariable[alias])
                {
                    foreach (var field in FieldsIn(rule))
                        VisitField(field);
                    foreach (var variable in VariablesIn(rule))
                        VisitVariable(variable);
                }
            }

            void VisitField(string alias)
            {
                if (!fields.Add(alias))
                    return;

                var pageIndex = _definition.FindPageIndex(alias);
                var formField = _definition.FindField(alias);
                var conditions = (formField is null ? [] : VisibilityConditions(formField))
                    .Append(pageIndex >= 0 ? _definition.Pages[pageIndex].Visibility : null);
                foreach (var condition in conditions)
                {
                    foreach (var field in FieldsIn(condition))
                        VisitField(field);
                    foreach (var variable in VariablesIn(condition))
                        VisitVariable(variable);
                }
            }

            VisitVariable(variableAlias);
            return fields;
        }
    }
}
