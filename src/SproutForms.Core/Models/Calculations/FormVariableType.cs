using System.Text.Json.Serialization;

namespace SproutForms.Core.Models.Calculations
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum FormVariableType
    {
        Number,
        Text
    }
}
