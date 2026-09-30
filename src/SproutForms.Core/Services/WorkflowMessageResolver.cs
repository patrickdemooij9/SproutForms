using System.Text.RegularExpressions;
using SproutForms.Core.Models;

namespace SproutForms.Core.Services
{
    /// <summary>
    /// Fills in the tokens of a workflow's message: {alias} for a field's value and {AllValues} for all of them.
    /// A field group such as a repeater has one token for all its entries; the fields inside it have none.
    /// </summary>
    public class WorkflowMessageResolver
    {
        private static readonly Regex TokenPattern = new Regex(@"\{([^}]+)\}", RegexOptions.Compiled);

        private readonly FormValueFormatter _formatter;

        public WorkflowMessageResolver(FormValueFormatter formatter)
        {
            _formatter = formatter;
        }

        public string ResolveTokens(string template, FormSubmission submission, FormVersion formVersion)
        {
            if (string.IsNullOrEmpty(template))
                return template;

            return TokenPattern.Replace(template, match =>
            {
                var fieldAlias = match.Groups[1].Value;
                return GetFieldValue(fieldAlias, submission, formVersion);
            });
        }

        private string GetFieldValue(string fieldAlias, FormSubmission submission, FormVersion formVersion)
        {
            var fields = formVersion.Definition.Fields;
            if (fieldAlias.Equals("AllValues", StringComparison.InvariantCultureIgnoreCase))
            {
                var values = _formatter.FormatAll(submission.Values, fields)
                    .Select(it => $"*{it.Label}:*\n{it.Value}");
                return string.Join("\r\n", values);
            }
            if (submission.Values.TryGetValue(fieldAlias, out var jsonElement))
            {
                return _formatter.Format(fields.FirstOrDefault(it => it.Alias == fieldAlias), jsonElement) ?? string.Empty;
            }

            return $"[{fieldAlias}]";
        }
    }
}
