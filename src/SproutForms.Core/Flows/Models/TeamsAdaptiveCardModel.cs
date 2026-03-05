using System.Text.Json.Serialization;

namespace SproutForms.Core.Flows.Models
{
    public class TeamsAdaptiveCardModel
    {
        [JsonPropertyName("$schema")]
        public string Schema => "http://adaptivecards.io/schemas/adaptive-card.json";

        [JsonPropertyName("type")]
        public string Type => "AdaptiveCard";

        [JsonPropertyName("version")]
        public string Version => "1.3";

        [JsonPropertyName("msteams")]
        public object Msteams => new { width = "full" };

        [JsonPropertyName("body")]
        public TeamsBodyElement[] Body { get; set; } = [];
    }
}
