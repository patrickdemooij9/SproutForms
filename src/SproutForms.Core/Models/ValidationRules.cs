using System.Text.Json.Serialization;

namespace SproutForms.Core.Models
{
    public class ValidationRule
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("value")]
        public object? Value { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }
    }
}
