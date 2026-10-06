using System.Text.Json.Serialization;

namespace SproutForms.Core.Models.Calculations
{
    /// <summary>
    /// What a calculation rule does to its variable. Numbers can be set, added to, subtracted from, multiplied and divided; text can be
    /// set and appended to.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum CalculationOperation
    {
        Set,
        Add,
        Subtract,
        Multiply,
        Divide,
        Append
    }
}
