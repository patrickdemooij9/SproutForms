using System.Text.Json;
using System.Text.RegularExpressions;
using SproutForms.Core.Models;

namespace SproutForms.Core.Services
{
    public class WorkflowMessageResolver
    {
        private static readonly Regex TokenPattern = new Regex(@"#([a-zA-Z_][a-zA-Z0-9_]*)", RegexOptions.Compiled);

        public static string ResolveTokens(string template, FormSubmission submission)
        {
            if (string.IsNullOrEmpty(template))
                return template;

            return TokenPattern.Replace(template, match =>
            {
                var fieldAlias = match.Groups[1].Value;
                return GetFieldValue(fieldAlias, submission);
            });
        }

        private static string GetFieldValue(string fieldAlias, FormSubmission submission)
        {
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
