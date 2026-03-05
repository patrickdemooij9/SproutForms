using System.Text.Json.Serialization;

namespace SproutForms.Core.Flows.Models
{
    public class TeamsTextBlockModel : TeamsBodyElement
    {
        [JsonPropertyName("type")]
        public string Type => "TextBlock";

        [JsonPropertyName("text")]
        public string Text { get; set; }
    }
}
