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

        // The number at the start of a value, as JavaScript's parseFloat reads it
        private static readonly Regex LeadingNumber = new(@"^\s*[+-]?(\d+\.?\d*|\.\d+)([eE][+-]?\d+)?", RegexOptions.Compiled);

        public bool IsVisible(FormField field, Dictionary<string, JsonElement> values)
            => field.Conditions?.Visibility is null
                || Evaluate(field.Conditions.Visibility, values);

        public bool IsVisible(FormPage page, Dictionary<string, JsonElement> values)
            => page.Visibility is null
                || Evaluate(page.Visibility, values);

        public bool IsRequired(FormField field, Dictionary<string, JsonElement> values)
            => field.Conditions?.Required is not null
                && Evaluate(field.Conditions.Required, values);

        private static bool Evaluate(
            ConditionDefinition condition,
            Dictionary<string, JsonElement> values)
        {
            if (condition.Rules.Count == 0)
                return true;

            return condition.Operator == "All"
                ? condition.Rules.All(r => EvaluateRule(r, values))
                : condition.Rules.Any(r => EvaluateRule(r, values));
        }

        private static bool EvaluateRule(
            ConditionRule rule,
            Dictionary<string, JsonElement> values)
        {
            // A field that wasn't posted counts as empty
            var value = values.TryGetValue(rule.FieldAlias, out var element) ? AsString(element) : "";
            var target = rule.Value is JsonElement targetElement ? AsString(targetElement) : rule.Value?.ToString() ?? "";

            return rule.Comparison switch
            {
                ConditionComparison.Equals => string.Equals(value, target, StringComparison.OrdinalIgnoreCase),
                ConditionComparison.NotEquals => !string.Equals(value, target, StringComparison.OrdinalIgnoreCase),
                ConditionComparison.Contains => value.Contains(target, StringComparison.OrdinalIgnoreCase),
                ConditionComparison.GreaterThan => ParseNumber(value) is { } greater && ParseNumber(target) is { } than && greater > than,
                ConditionComparison.LessThan => ParseNumber(value) is { } less && ParseNumber(target) is { } lessThan && less < lessThan,
                ConditionComparison.IsEmpty => string.IsNullOrWhiteSpace(value),
                ConditionComparison.IsNotEmpty => !string.IsNullOrWhiteSpace(value),
                ConditionComparison.MatchesRegex => MatchesRegex(value, target) == true,
                ConditionComparison.DoesNotMatchRegex => MatchesRegex(value, target) == false,
                _ => true
            };
        }

        private static string AsString(JsonElement element)
            => element.ValueKind switch
            {
                JsonValueKind.String => element.GetString() ?? "",
                JsonValueKind.Null or JsonValueKind.Undefined => "",
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                _ => element.GetRawText()
            };

        private static double? ParseNumber(string value)
        {
            var match = LeadingNumber.Match(value);
            return match.Success && double.TryParse(match.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number)
                ? number
                : null;
        }

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
