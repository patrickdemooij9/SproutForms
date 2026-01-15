using System.Text.Json;
using System.Text.RegularExpressions;

namespace SproutForms.Core.Models.Conditions
{
    public sealed class ConditionEvaluator : IConditionEvaluator
    {
        public bool IsVisible(FormField field, Dictionary<string, JsonElement> values)
            => field.Conditions?.Visibility is null
                || Evaluate(field.Conditions.Visibility, values);

        public bool IsRequired(FormField field, Dictionary<string, JsonElement> values)
            => field.Conditions?.Required is not null
                && Evaluate(field.Conditions.Required, values);

        private bool Evaluate(
            ConditionDefinition condition,
            Dictionary<string, JsonElement> values)
        {
            return condition.Operator == "All"
                ? condition.Rules.All(r => EvaluateRule(r, values))
                : condition.Rules.Any(r => EvaluateRule(r, values));
        }

        private bool EvaluateRule(
            ConditionRule rule,
            Dictionary<string, JsonElement> values)
        {
            if (!values.TryGetValue(rule.FieldAlias, out var value))
                return false;

            var str = value.GetString() ?? "";

            return rule.Comparison switch
            {
                ConditionComparison.Equals => str == rule.Value?.ToString(),
                ConditionComparison.Contains => str.Contains(rule.Value?.ToString()!),
                ConditionComparison.MatchesRegex =>
                    Regex.IsMatch(str, rule.Value!.ToString()!),
                _ => false
            };
        }
    }

}
