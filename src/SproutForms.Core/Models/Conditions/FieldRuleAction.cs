using System.Text.Json.Serialization;

namespace SproutForms.Core.Models.Conditions
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum FieldRuleAction
    {
        Show,
        Hide,
        Require
    }
}
