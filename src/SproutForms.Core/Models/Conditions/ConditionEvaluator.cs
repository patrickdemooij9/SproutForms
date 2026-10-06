using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SproutForms.Core.Models.Conditions
{
    /// <summary>
    /// Evaluates conditions the same way forms.js does in the browser, so a field or page the visitor saw is the one the server validates.
    /// </summary>
    public sealed class ConditionEvaluator : IConditionEvaluator
    {
        private static readonly TimeSpan RegexMatchTimeout = TimeSpan.FromMilliseconds(500);

        private static readonly IReadOnlyDictionary<string, JsonElement> NoVariables = new Dictionary<string, JsonElement>();

        public bool IsVisible(FormField field, IReadOnlyDictionary<string, JsonElement> values, IReadOnlyDictionary<string, JsonElement>? variables = null)
        {
            if (field.Rules.Any(rule => rule.Action == FieldRuleAction.Hide && Evaluate(rule.Condition, values, variables)))
                return false;

            var showRules = field.Rules.Where(rule => rule.Action == FieldRuleAction.Show).ToList();
            return showRules.Count == 0 || showRules.Any(rule => Evaluate(rule.Condition, values, variables));
        }

        public bool IsVisible(FormPage page, IReadOnlyDictionary<string, JsonElement> values, IReadOnlyDictionary<string, JsonElement>? variables = null)
            => page.Visibility is null
                || Evaluate(page.Visibility, values, variables);

        public bool IsRequired(FormField field, IReadOnlyDictionary<string, JsonElement> values, IReadOnlyDictionary<string, JsonElement>? variables = null)
            => field.Rules.Any(rule => rule.Action == FieldRuleAction.Require && Evaluate(rule.Condition, values, variables));

        public bool Evaluate(ConditionDefinition? condition, IReadOnlyDictionary<string, JsonElement> values, IReadOnlyDictionary<string, JsonElement>? variables = null)
        {
            if (condition is null || condition.Rules.Count == 0)
                return true;

            variables ??= NoVariables;
            return condition.Operator == "All"
                ? condition.Rules.All(r => EvaluateRule(r, values, variables))
                : condition.Rules.Any(r => EvaluateRule(r, values, variables));
        }

        private static bool EvaluateRule(
            ConditionRule rule,
            IReadOnlyDictionary<string, JsonElement> values,
            IReadOnlyDictionary<string, JsonElement> variables)
        {
            // A field that wasn't posted, or a variable the form doesn't have, counts as empty
            var value = rule.VariableAlias is { Length: > 0 } variableAlias
                ? Read(variables, variableAlias)
                : Read(values, rule.FieldAlias);
            var target = rule.ValueSource switch
            {
                ConditionValueSource.Field => Read(values, AsString(rule.Value)),
                ConditionValueSource.Variable => Read(variables, AsString(rule.Value)),
                _ => AsString(rule.Value)
            };

            return rule.Comparison switch
            {
                ConditionComparison.Equals => string.Equals(value, target, StringComparison.OrdinalIgnoreCase),
                ConditionComparison.NotEquals => !string.Equals(value, target, StringComparison.OrdinalIgnoreCase),
                ConditionComparison.Contains => value.Contains(target, StringComparison.OrdinalIgnoreCase),
                ConditionComparison.GreaterThan => ConditionValues.ParseNumber(value) is { } greater && ConditionValues.ParseNumber(target) is { } than && greater > than,
                ConditionComparison.LessThan => ConditionValues.ParseNumber(value) is { } less && ConditionValues.ParseNumber(target) is { } lessThan && less < lessThan,
                ConditionComparison.IsEmpty => string.IsNullOrWhiteSpace(value),
                ConditionComparison.IsNotEmpty => !string.IsNullOrWhiteSpace(value),
                ConditionComparison.MatchesRegex => MatchesRegex(value, target) == true,
                ConditionComparison.DoesNotMatchRegex => MatchesRegex(value, target) == false,
                _ => true
            };
        }

        private static string Read(IReadOnlyDictionary<string, JsonElement> values, string alias)
            => values.TryGetValue(alias, out var element) ? ConditionValues.AsString(element) : "";

        private static string AsString(object? value)
            => value is JsonElement element ? ConditionValues.AsString(element) : Convert.ToString(value, CultureInfo.InvariantCulture) ?? "";

        // Null when the pattern is invalid or too slow, which fails both regex comparisons, as in the browser
        private static bool? MatchesRegex(string value, string pattern)
        {
            try
            {
                return Regex.IsMatch(value, pattern, RegexOptions.None, RegexMatchTimeout);
            }
            catch (ArgumentException)
            {
                return null;
            }
            catch (RegexMatchTimeoutException)
            {
                return null;
            }
        }
    }

}
