using SproutForms.Core.Models;

namespace SproutForms.Core.Services
{
    /// <summary>
    /// Checks the layout of a definition: every field sits on exactly one page, and conditions only look back at answers the visitor has already given.
    /// </summary>
    public static class FormDefinitionStructureValidator
    {
        /// <summary>
        /// Returns a message per problem, or none when the layout is valid.
        /// </summary>
        public static IReadOnlyList<string> Validate(FormDefinition definition)
        {
            var errors = new List<string>();
            if (definition.Pages.Count == 0)
            {
                errors.Add("The form has no pages.");
                return errors;
            }

            // The field labels, because backoffice fields have generated aliases
            var labels = definition.Fields.ToDictionary(field => field.Alias, field => field.Label);
            string Label(string alias) => labels.TryGetValue(alias, out var label) ? label : alias;

            var pageIndexByAlias = new Dictionary<string, int>();
            for (var index = 0; index < definition.Pages.Count; index++)
            {
                var aliases = definition.Pages[index].Rows.SelectMany(row => row.Columns).Select(column => column.FieldAlias).ToList();

                // A form with a single page may be empty while it's being built; with more pages, an empty one would be a step with nothing on it
                if (aliases.Count == 0 && definition.Pages.Count > 1)
                {
                    errors.Add($"{PageName(definition, index)} has no fields.");
                }

                foreach (var alias in aliases)
                {
                    if (!labels.ContainsKey(alias))
                        errors.Add($"{PageName(definition, index)} places field '{alias}', which the form doesn't have.");
                    else if (!pageIndexByAlias.TryAdd(alias, index))
                        errors.Add($"Field '{Label(alias)}' is placed more than once.");
                }
            }

            foreach (var field in definition.Fields.Where(field => !pageIndexByAlias.ContainsKey(field.Alias)))
            {
                errors.Add($"Field '{field.Label}' isn't placed on any page.");
            }

            foreach (var field in definition.Fields.Where(field => pageIndexByAlias.ContainsKey(field.Alias)))
            {
                var fieldPage = pageIndexByAlias[field.Alias];
                var rules = new[] { field.Conditions?.Visibility, field.Conditions?.Required }
                    .Where(condition => condition != null)
                    .SelectMany(condition => condition!.Rules);
                foreach (var rule in rules)
                {
                    if (!pageIndexByAlias.TryGetValue(rule.FieldAlias, out var rulePage))
                        errors.Add($"A condition of field '{field.Label}' uses field '{rule.FieldAlias}', which isn't on the form.");
                    else if (rulePage > fieldPage)
                        errors.Add($"A condition of field '{field.Label}' uses field '{Label(rule.FieldAlias)}', which is on a later page.");
                }
            }

            for (var index = 0; index < definition.Pages.Count; index++)
            {
                foreach (var rule in definition.Pages[index].Visibility?.Rules ?? [])
                {
                    if (!pageIndexByAlias.TryGetValue(rule.FieldAlias, out var rulePage))
                        errors.Add($"A condition of {PageName(definition, index)} uses field '{rule.FieldAlias}', which isn't on the form.");
                    else if (rulePage >= index)
                        errors.Add($"A condition of {PageName(definition, index)} uses field '{Label(rule.FieldAlias)}', which isn't on an earlier page.");
                }
            }

            return errors;
        }

        private static string PageName(FormDefinition definition, int index)
            => string.IsNullOrWhiteSpace(definition.Pages[index].Title)
                ? $"Page {index + 1}"
                : $"Page {index + 1} ('{definition.Pages[index].Title}')";
    }
}
