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

        private static string GetFieldValue(string fieldAlias, FormSubmission submission, FormVersion formVersion)
        {
            if (fieldAlias.Equals("AllValues", StringComparison.InvariantCultureIgnoreCase))
            {
                var fields = submission.Values
                .Where(v => !string.IsNullOrEmpty(v.Value.GetString()))
                .Select(v => $"*{formVersion.Definition.Fields.First(it => it.Alias == v.Key).Label}:*\n{v.Value.GetString()}")
                .ToList();
                return string.Join("\r\n", fields);
            }
            if (submission.Values.TryGetValue(fieldAlias, out var jsonElement))
            {
                try
                {
                    if (jsonElement.ValueKind == JsonValueKind.String)
                        return jsonElement.GetString() ?? string.Empty;

                    return jsonElement.GetRawText();
                }
                catch
                {
                    return string.Empty;
                }
            }

            return $"[{fieldAlias}]";
        }
    }
}
