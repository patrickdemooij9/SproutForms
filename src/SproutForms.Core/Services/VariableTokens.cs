using SproutForms.Core.Models;
using SproutForms.Core.Models.Calculations;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SproutForms.Core.Services
{
    /// <summary>
    /// Fills in {var:alias} tokens with the value of a submission's variables. A number shows as many decimals as its variable keeps,
    /// and a variable the form doesn't have shows as nothing.
    /// </summary>
    public static class VariableTokens
    {
        public const string Prefix = "var:";

        private static readonly Regex TokenPattern = new(@"\{var:([^{}]+)\}", RegexOptions.Compiled);

        /// <param name="encode">Encodes each value for where the text goes, such as HTML or a URL</param>
        public static string Resolve(string? template, FormSubmission submission, FormDefinition definition, Func<string, string>? encode = null)
        {
            if (string.IsNullOrEmpty(template))
                return template ?? string.Empty;

            return TokenPattern.Replace(template, match =>
            {
                var value = Format(match.Groups[1].Value.Trim(), submission, definition);
                return encode is null ? value : encode(value);
            });
        }

        /// <summary>
        /// A variable's value as text, or empty when the submission doesn't have it.
        /// </summary>
        public static string Format(string alias, FormSubmission submission, FormDefinition definition)
        {
            if (!submission.Variables.TryGetValue(alias, out var value))
                return string.Empty;

            var variable = definition.Variables.FirstOrDefault(it => it.Alias == alias);
            if (variable?.Type != FormVariableType.Number || value.ValueKind != JsonValueKind.Number)
                return VariableValues.ToText(value);

            var decimals = Math.Clamp(variable.Decimals, 0, VariableValues.MaxDecimals);
            return VariableValues.ToNumber(value).ToString("F" + decimals, CultureInfo.InvariantCulture);
        }
    }
}
