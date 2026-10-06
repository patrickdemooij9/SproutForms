using System.Text.Json;

namespace SproutForms.Core.Models.Conditions
{
    /// <summary>
    /// Evaluates conditions against the answers, by field alias, and the form's variables, by variable alias.
    /// </summary>
    public interface IConditionEvaluator
    {
        bool IsVisible(FormField field, IReadOnlyDictionary<string, JsonElement> values, IReadOnlyDictionary<string, JsonElement>? variables = null);
        bool IsVisible(FormPage page, IReadOnlyDictionary<string, JsonElement> values, IReadOnlyDictionary<string, JsonElement>? variables = null);
        // Required by one of the field's rules; FormField.Required is checked separately
        bool IsRequired(FormField field, IReadOnlyDictionary<string, JsonElement> values, IReadOnlyDictionary<string, JsonElement>? variables = null);

        /// <summary>
        /// No condition, or one without rules, holds.
        /// </summary>
        bool Evaluate(ConditionDefinition? condition, IReadOnlyDictionary<string, JsonElement> values, IReadOnlyDictionary<string, JsonElement>? variables = null);
    }
}
