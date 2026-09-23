using SproutForms.Core.Models;
using SproutForms.Core.Models.FormTypes;
using SproutForms.Core.Models.Outcomes;

namespace SproutForms.Core.Services
{
    /// <summary>
    /// Checks that a definition only uses field and outcome types its form type allows, and only field extensions it defines.
    /// </summary>
    public class FormDefinitionTypeValidator
    {
        private readonly IFormDefinitionType[] _formTypes;
        private readonly IFormFieldType[] _fieldTypes;
        private readonly IFormSubmitOutcomeType[] _outcomeTypes;

        public FormDefinitionTypeValidator(
            IEnumerable<IFormDefinitionType> formTypes,
            IEnumerable<IFormFieldType> fieldTypes,
            IEnumerable<IFormSubmitOutcomeType> outcomeTypes)
        {
            _formTypes = formTypes.ToArray();
            _fieldTypes = fieldTypes.ToArray();
            _outcomeTypes = outcomeTypes.ToArray();
        }

        public static bool IsAllowed(IFormDefinitionType formType, IFormFieldType fieldType)
        {
            return IsAllowedByRestriction(formType, fieldType) && formType.AllowsFieldType(fieldType);
        }

        public static bool IsAllowed(IFormDefinitionType formType, IFormSubmitOutcomeType outcomeType)
        {
            return IsAllowedByRestriction(formType, outcomeType) && formType.AllowsOutcomeType(outcomeType);
        }

        /// <summary>
        /// Returns a message per problem, or none when the definition is valid. Field and outcome types that aren't registered are
        /// left to the code that resolves them.
        /// </summary>
        public IReadOnlyList<string> Validate(FormDefinition definition)
        {
            var typeAlias = definition.Type.TypeAlias;
            var formType = _formTypes.FirstOrDefault(it => it.Alias == typeAlias);
            if (formType is null)
                return [$"The form uses form type '{typeAlias}', which is not registered."];

            var errors = new List<string>();
            if (!formType.SettingsType.IsInstanceOfType(definition.Type.Settings))
            {
                errors.Add($"The settings of form type '{typeAlias}' must be a {formType.SettingsType.Name}.");
            }

            foreach (var field in definition.Fields)
            {
                var fieldType = _fieldTypes.FirstOrDefault(it => it.Alias == field.FieldTypeAlias);
                if (fieldType != null && !IsAllowed(formType, fieldType))
                {
                    // The label, because backoffice fields have generated aliases
                    errors.Add($"Field '{field.Label}' uses field type '{fieldType.Alias}', which form type '{typeAlias}' doesn't allow.");
                }

                if (field.Extension is null) continue;

                var extension = formType.FieldExtensions.FirstOrDefault(it => it.FieldTypeAlias == field.FieldTypeAlias);
                if (field.Extension.FormTypeAlias != typeAlias || extension is null)
                {
                    errors.Add($"Field '{field.Label}' has extension settings, but form type '{typeAlias}' doesn't extend field type '{field.FieldTypeAlias}'.");
                }
                else if (!extension.SettingsType.IsInstanceOfType(field.Extension.Settings))
                {
                    errors.Add($"The extension settings of field '{field.Label}' must be a {extension.SettingsType.Name}.");
                }
            }

            var outcomeType = _outcomeTypes.FirstOrDefault(it => it.Alias == definition.SubmitOutcome.OutcomeTypeAlias);
            if (outcomeType != null && !IsAllowed(formType, outcomeType))
            {
                errors.Add($"Submit outcome type '{outcomeType.Alias}' isn't allowed by form type '{typeAlias}'.");
            }

            return errors;
        }

        private static bool IsAllowedByRestriction(IFormDefinitionType formType, object type)
        {
            return type is not IRestrictedToFormTypes restricted || restricted.FormTypeAliases.Contains(formType.Alias);
        }
    }
}
