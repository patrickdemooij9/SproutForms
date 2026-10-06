using SproutForms.Core.Models;
using SproutForms.Core.Models.Calculations;
using SproutForms.Core.Models.Conditions;
using System.Text.RegularExpressions;

namespace SproutForms.Core.Services
{
    /// <summary>
    /// Checks the layout of a definition: every field sits on exactly one page or in one field group, and conditions only look back at answers the visitor has already given.
    /// </summary>
    public static class FormDefinitionStructureValidator
    {
        // How deep field groups may go: a repeater can't hold another repeater yet
        public const int MaxFieldGroupDepth = 1;

        // A variable's alias goes in {var:alias} tokens, so it keeps to letters, digits, underscores and dashes
        private static readonly Regex VariableAliasPattern = new(@"^[A-Za-z_][A-Za-z0-9_-]*$", RegexOptions.Compiled);

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
                foreach (var alias in field.Rules.SelectMany(rule => CalculationDependencies.FieldsIn(rule.Condition)).Distinct())
                {
                    if (availableByAlias[field.Alias].Contains(alias))
                        continue;

                    if (groupByChildAlias.TryGetValue(alias, out var group))
                        errors.Add($"A condition of field '{field.Label}' uses field '{Label(alias)}', which is inside '{group.Label}'; only the fields in the same entry can use it.");
                    else if (!pageIndexByAlias.ContainsKey(alias))
                        errors.Add($"A condition of field '{field.Label}' uses field '{alias}', which isn't on the form.");
                    else
                        errors.Add($"A condition of field '{field.Label}' uses field '{Label(alias)}', which is on a later page.");
                }
            }

            for (var index = 0; index < definition.Pages.Count; index++)
            {
                foreach (var alias in CalculationDependencies.FieldsIn(definition.Pages[index].Visibility))
                {
                    if (!pageIndexByAlias.TryGetValue(alias, out var rulePage))
                        errors.Add(groupByChildAlias.TryGetValue(alias, out var group)
                            ? $"A condition of {PageName(definition, index)} uses field '{Label(alias)}', which is inside '{group.Label}'."
                            : $"A condition of {PageName(definition, index)} uses field '{alias}', which isn't on the form.");
                    else if (rulePage >= index)
                        errors.Add($"A condition of {PageName(definition, index)} uses field '{Label(alias)}', which isn't on an earlier page.");
                }
            }

            ValidateCalculations(definition, allFields, pageIndexByAlias, availableByAlias, groupByChildAlias, Label, errors);

            return errors;
        }

        /// <summary>
        /// Variables have valid, unique aliases, rules and conditions only use fields and variables the form has, and a condition on a
        /// field or page only uses a variable whose value can't depend on that field itself or on a later page, the same as with fields.
        /// </summary>
        private static void ValidateCalculations(
            FormDefinition definition,
            List<FormField> allFields,
            Dictionary<string, int> pageIndexByAlias,
            Dictionary<string, HashSet<string>> availableByAlias,
            Dictionary<string, FormField> groupByChildAlias,
            Func<string, string> label,
            List<string> errors)
        {
            var variables = new Dictionary<string, FormVariable>();
            foreach (var variable in definition.Variables)
            {
                if (!VariableAliasPattern.IsMatch(variable.Alias ?? string.Empty))
                    errors.Add($"Variable '{variable.Alias}' needs an alias of letters, digits, underscores and dashes that doesn't start with a digit or dash.");
                else if (!variables.TryAdd(variable.Alias, variable))
                    errors.Add($"The form has more than one variable with alias '{variable.Alias}'.");

                if (variable.Decimals < 0 || variable.Decimals > VariableValues.MaxDecimals)
                    errors.Add($"Variable '{variable.Alias}' can keep 0 to {VariableValues.MaxDecimals} decimals.");
            }

            // Calculations and outcomes read the fields of the form itself; a field group holds a list of entries
            void CheckField(string alias, string user)
            {
                if (groupByChildAlias.TryGetValue(alias, out var group))
                    errors.Add($"{user} uses field '{label(alias)}', which is inside '{group.Label}'.");
                else if (!pageIndexByAlias.ContainsKey(alias))
                    errors.Add($"{user} uses field '{alias}', which isn't on the form.");
            }

            void CheckVariables(IEnumerable<string> aliases, string user)
            {
                foreach (var alias in aliases.Where(alias => !variables.ContainsKey(alias)).Distinct())
                    errors.Add($"{user} uses variable '{alias}', which the form doesn't have.");
            }

            for (var index = 0; index < definition.Calculations.Count; index++)
            {
                var rule = definition.Calculations[index];
                var user = $"Calculation {index + 1}";
                if (!variables.TryGetValue(rule.VariableAlias ?? string.Empty, out var target))
                    errors.Add($"{user} changes variable '{rule.VariableAlias}', which the form doesn't have.");
                else if (!FitsType(rule.Operation, target.Type))
                    errors.Add($"{user} can't {rule.Operation.ToString().ToLowerInvariant()} variable '{target.Alias}', which holds {(target.Type == FormVariableType.Text ? "text" : "a number")}.");

                foreach (var alias in CalculationDependencies.FieldsIn(rule))
                    CheckField(alias, user);
                CheckVariables(CalculationDependencies.VariablesIn(rule), user);

                // Only the form's own fields list rules that change a variable
                if (rule.OwnerFieldAlias is { Length: > 0 } owner && !pageIndexByAlias.ContainsKey(owner))
                    errors.Add($"{user} is listed on field '{label(owner)}', which isn't one of the form's own fields.");
            }

            for (var index = 0; index < definition.ConditionalOutcomes.Count; index++)
            {
                var outcome = definition.ConditionalOutcomes[index];
                var user = $"Conditional outcome {index + 1}";
                if (outcome.Condition.Rules.Count == 0)
                    errors.Add($"{user} has no condition; make it the outcome after submitting instead.");

                foreach (var alias in CalculationDependencies.FieldsIn(outcome.Condition))
                    CheckField(alias, user);
                CheckVariables(CalculationDependencies.VariablesIn(outcome.Condition), user);
            }

            var dependencies = new CalculationDependencies(definition);
            foreach (var field in allFields.Where(field => availableByAlias.ContainsKey(field.Alias)))
            {
                var user = $"A condition of field '{field.Label}'";
                var visibilityVariables = CalculationDependencies.VisibilityConditions(field).SelectMany(CalculationDependencies.VariablesIn).ToList();
                var usedVariables = field.Rules.SelectMany(rule => CalculationDependencies.VariablesIn(rule.Condition)).Distinct().ToList();
                CheckVariables(usedVariables, user);

                // A hidden field counts as empty, so a field whose visibility depends on its own value would never settle
                foreach (var alias in visibilityVariables.Where(variables.ContainsKey).Distinct())
                {
                    if (dependencies.GetFieldsBehind(alias).Contains(field.Alias))
                        errors.Add($"{user} uses variable '{alias}', which depends on field '{field.Label}' itself.");
                }

                foreach (var alias in usedVariables.Where(variables.ContainsKey))
                {
                    foreach (var later in dependencies.GetFieldsBehind(alias).Where(it => it != field.Alias && !availableByAlias[field.Alias].Contains(it)))
                        errors.Add($"{user} uses variable '{alias}', which depends on field '{label(later)}' on a later page.");
                }
            }

            for (var index = 0; index < definition.Pages.Count; index++)
            {
                var user = $"A condition of {PageName(definition, index)}";
                var used = CalculationDependencies.VariablesIn(definition.Pages[index].Visibility).Distinct().ToList();
                CheckVariables(used, user);

                foreach (var alias in used.Where(variables.ContainsKey))
                {
                    foreach (var later in dependencies.GetFieldsBehind(alias).Where(it => !pageIndexByAlias.TryGetValue(it, out var page) || page >= index))
                        errors.Add($"{user} uses variable '{alias}', which depends on field '{label(later)}', which isn't on an earlier page.");
                }
            }
        }

        private static bool FitsType(CalculationOperation operation, FormVariableType type)
            => type == FormVariableType.Text
                ? operation is CalculationOperation.Set or CalculationOperation.Append
                : operation is not CalculationOperation.Append;

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

        private static string PageName(FormDefinition definition, int index)
            => string.IsNullOrWhiteSpace(definition.Pages[index].Title)
                ? $"Page {index + 1}"
                : $"Page {index + 1} ('{definition.Pages[index].Title}')";
    }
}
