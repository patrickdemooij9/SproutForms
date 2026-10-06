using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SproutForms.Core.Models.Conditions
{
    /// <summary>
    /// Reads values the way conditions and calculations compare them, the same as the browser does.
    /// </summary>
    public static class ConditionValues
    {
        // The number at the start of a value, as JavaScript's parseFloat reads it
        private static readonly Regex LeadingNumber = new(@"^\s*[+-]?(\d+\.?\d*|\.\d+)([eE][+-]?\d+)?", RegexOptions.Compiled);

        /// <summary>
        /// A value as text: booleans as "true"/"false", and a missing value as empty.
        /// </summary>
        public static string AsString(JsonElement element)
            => element.ValueKind switch
            {
                JsonValueKind.String => element.GetString() ?? "",
                JsonValueKind.Null or JsonValueKind.Undefined => "",
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                _ => element.GetRawText()
            };

        public static double? ParseNumber(string value)
        {
            var match = LeadingNumber.Match(value);
            return match.Success && double.TryParse(match.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number)
                ? number
                : null;
        }

        /// <summary>
        /// The number at the start of a value, as a decimal for calculations; null when it doesn't start with one, or it doesn't fit.
        /// </summary>
        public static decimal? ParseDecimal(string value)
        {
            var match = LeadingNumber.Match(value);
            return match.Success && decimal.TryParse(match.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number)
                ? number
                : null;
        }
    }
}
