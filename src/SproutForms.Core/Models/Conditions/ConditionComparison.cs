using System.Text.Json.Serialization;

namespace SproutForms.Core.Models.Conditions
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ConditionComparison
    {
        Equals,
        NotEquals,
        Contains,
        GreaterThan,
        LessThan,
        IsEmpty,
        IsNotEmpty,
        MatchesRegex,
        DoesNotMatchRegex
    }
}
