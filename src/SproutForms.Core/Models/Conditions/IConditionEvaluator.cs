using System.Text.Json;

namespace SproutForms.Core.Models.Conditions
{
    public interface IConditionEvaluator
    {
        bool IsVisible(FormField field, Dictionary<string, JsonElement> values);
        bool IsRequired(FormField field, Dictionary<string, JsonElement> values);
    }
}
