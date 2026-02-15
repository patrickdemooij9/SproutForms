using System.Text.Json.Serialization;

namespace SproutForms.Core.Models.Conditions
{
    public class FieldConditions
    {
        public ConditionDefinition? Visibility { get; init; }
        public ConditionDefinition? Required { get; init; }
    }
}
