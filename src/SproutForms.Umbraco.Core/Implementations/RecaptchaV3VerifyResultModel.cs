using System.Text.Json.Serialization;

namespace SproutForms.Umbraco.Core.Implementations
{
    internal class RecaptchaV3VerifyResultModel
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("score")]
        public double Score { get; set; }

        [JsonPropertyName("action")]
        public string? Action { get; set; }
    }
}
