using SproutForms.Core.Models;
using SproutForms.Core.Models.Conditions;

namespace SproutForms.Core.Services
{
    /// <summary>
    /// Checks the layout of a definition: every field sits on exactly one page or in one field group, and conditions only look back at answers the visitor has already given.
    /// </summary>
    public static class FormDefinitionStructureValidator
    {
        // How deep field groups may go: a repeater can't hold another repeater yet
        public const int MaxFieldGroupDepth = 1;

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

            // Values, errors and conditions find a field by its alias, at any depth
            var allFields = definition.GetAllFields().ToList();
            foreach (var duplicate in allFields.GroupBy(field => field.Alias).Where(group => group.Count() > 1))
            {
                errors.Add($"The form has more than one field with alias '{duplicate.Key}'.");
            }

            // The field labels, because backoffice fields have generated aliases
            var labels = allFields.GroupBy(field => field.Alias).ToDictionary(group => group.Key, group => group.First().Label);
            string Label(string alias) => labels.TryGetValue(alias, out var label) ? label : alias;

            var topLevelAliases = definition.Fields.Select(field => field.Alias).ToHashSet();
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
                    if (!topLevelAliases.Contains(alias))
                        errors.Add($"{PageName(definition, index)} places field '{alias}', which the form doesn't have.");
                    else if (!pageIndexByAlias.TryAdd(alias, index))
                        errors.Add($"Field '{Label(alias)}' is placed more than once.");
                }
            }

            foreach (var field in definition.Fields.Where(field => !pageIndexByAlias.ContainsKey(field.Alias)))
            {
                errors.Add($"Field '{field.Label}' isn't placed on any page.");
            }

            // The fields a condition may use, per field: the fields on the same or an earlier page, and inside a field group, the other fields of its entry
            var availableByAlias = new Dictionary<string, HashSet<string>>();
            foreach (var field in definition.Fields.Where(field => pageIndexByAlias.ContainsKey(field.Alias)))
            {
                var fieldPage = pageIndexByAlias[field.Alias];
                var available = pageIndexByAlias.Where(it => it.Value <= fieldPage).Select(it => it.Key).ToHashSet();
                availableByAlias[field.Alias] = available;
                ValidateGroup(field, available, depth: 1, labels, availableByAlias, errors);
            }

            var groupByChildAlias = allFields
                .Where(field => field.Configuration is IFormFieldGroupConfiguration)
                .SelectMany(group => ((IFormFieldGroupConfiguration)group.Configuration).Fields.Select(child => (Child: child.Alias, Group: group)))
                .GroupBy(it => it.Child)
                .ToDictionary(it => it.Key, it => it.First().Group);

            foreach (var field in allFields.Where(field => availableByAlias.ContainsKey(field.Alias)))
            {
                foreach (var rule in GetRules(field.Conditions?.Visibility, field.Conditions?.Required))
                {
                    if (availableByAlias[field.Alias].Contains(rule.FieldAlias))
                        continue;

                    if (groupByChildAlias.TryGetValue(rule.FieldAlias, out var group))
                        errors.Add($"A condition of field '{field.Label}' uses field '{Label(rule.FieldAlias)}', which is inside '{group.Label}'; only the fields in the same entry can use it.");
                    else if (!pageIndexByAlias.ContainsKey(rule.FieldAlias))
                        errors.Add($"A condition of field '{field.Label}' uses field '{rule.FieldAlias}', which isn't on the form.");
                    else
                        errors.Add($"A condition of field '{field.Label}' uses field '{Label(rule.FieldAlias)}', which is on a later page.");
                }
            }

            for (var index = 0; index < definition.Pages.Count; index++)
            {
                foreach (var rule in definition.Pages[index].Visibility?.Rules ?? [])
                {
                    if (!pageIndexByAlias.TryGetValue(rule.FieldAlias, out var rulePage))
                        errors.Add(groupByChildAlias.TryGetValue(rule.FieldAlias, out var group)
                            ? $"A condition of {PageName(definition, index)} uses field '{Label(rule.FieldAlias)}', which is inside '{group.Label}'."
                            : $"A condition of {PageName(definition, index)} uses field '{rule.FieldAlias}', which isn't on the form.");
                    else if (rulePage >= index)
                        errors.Add($"A condition of {PageName(definition, index)} uses field '{Label(rule.FieldAlias)}', which isn't on an earlier page.");
                }
            }

            return errors;
        }

        // A field group's own layout places each of its fields once, and its fields' conditions may use the fields its own conditions may, and those of the same entry
        private static void ValidateGroup(
            FormField field,
            HashSet<string> available,
            int depth,
            IReadOnlyDictionary<string, string> labels,
            Dictionary<string, HashSet<string>> availableByAlias,
            List<string> errors)
        {
            if (field.Configuration is not IFormFieldGroupConfiguration group)
                return;

            if (depth > MaxFieldGroupDepth)
            {
                errors.Add($"Field '{field.Label}' can't be inside another field group.");
                return;
            }

            var childAliases = group.Fields.Select(child => child.Alias).ToHashSet();
            var placed = new HashSet<string>();
            foreach (var alias in group.Rows.SelectMany(row => row.Columns).Select(column => column.FieldAlias))
            {
                if (!childAliases.Contains(alias))
                    errors.Add($"Field '{field.Label}' places field '{alias}', which it doesn't have.");
                else if (!placed.Add(alias))
                    errors.Add($"Field '{labels[alias]}' is placed more than once in '{field.Label}'.");
            }

            var entryAvailable = new HashSet<string>(available);
            entryAvailable.UnionWith(childAliases);
            foreach (var child in group.Fields)
            {
                if (!placed.Contains(child.Alias))
                    errors.Add($"Field '{child.Label}' isn't placed in '{field.Label}'.");

                availableByAlias[child.Alias] = entryAvailable;
                ValidateGroup(child, entryAvailable, depth + 1, labels, availableByAlias, errors);
            }
        }

        private static IEnumerable<ConditionRule> GetRules(params ConditionDefinition?[] conditions)
            => conditions.Where(condition => condition != null).SelectMany(condition => condition!.Rules);

        private static string PageName(FormDefinition definition, int index)
            => string.IsNullOrWhiteSpace(definition.Pages[index].Title)
                ? $"Page {index + 1}"
                : $"Page {index + 1} ('{definition.Pages[index].Title}')";
    }
}
