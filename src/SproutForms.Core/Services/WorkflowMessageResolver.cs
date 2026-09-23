using System.Text.Json;
using System.Text.RegularExpressions;
using SproutForms.Core.Models;

namespace SproutForms.Core.Services
{
    public class WorkflowMessageResolver
    {
        private static readonly Regex TokenPattern = new Regex(@"\{([^}]+)\}", RegexOptions.Compiled);

        public static string ResolveTokens(string template, FormSubmission submission, FormVersion formVersion)
        {
            if (string.IsNullOrEmpty(template))
                return template;

            return TokenPattern.Replace(template, match =>
            {
                var fieldAlias = match.Groups[1].Value;
                return GetFieldValue(fieldAlias, submission, formVersion);
            });
        }

        /// <summary>
        /// The text to show for a submitted value, or null when the field was left empty.
        /// </summary>
        public static string? GetDisplayValue(JsonElement value)
        {
            var text = value.ValueKind switch
            {
                JsonValueKind.Null or JsonValueKind.Undefined => null,
                JsonValueKind.String => value.GetString(),
                _ => value.GetRawText()
            };

            return string.IsNullOrEmpty(text) ? null : text;
        }

        private static string GetFieldValue(string fieldAlias, FormSubmission submission, FormVersion formVersion)
        {
            if (fieldAlias.Equals("AllValues", StringComparison.InvariantCultureIgnoreCase))
            {
                var fields = submission.Values
                    .Select(v => (Alias: v.Key, Value: GetDisplayValue(v.Value)))
                    .Where(v => v.Value is not null)
                    .Select(v => $"*{GetLabel(v.Alias, formVersion)}:*\n{v.Value}")
                    .ToList();
                return string.Join("\r\n", fields);
            }
            if (submission.Values.TryGetValue(fieldAlias, out var jsonElement))
            {
                return GetDisplayValue(jsonElement) ?? string.Empty;
            }

            return $"[{fieldAlias}]";
        }

        // A value can outlive its field when the stored submission predates the current definition
        private static string GetLabel(string fieldAlias, FormVersion formVersion)
        {
            return formVersion.Definition.Fields.FirstOrDefault(it => it.Alias == fieldAlias)?.Label ?? fieldAlias;
        }
    }
}
