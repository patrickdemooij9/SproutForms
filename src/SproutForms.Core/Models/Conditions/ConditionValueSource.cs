using System.Text.Json.Serialization;

namespace SproutForms.Core.Models.Conditions
{
    /// <summary>
    /// Where a value in a condition or a calculation comes from: the value as written, or the value of a field or a variable.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ConditionValueSource
    {
        Value,
        Field,
        Variable
    }
}
