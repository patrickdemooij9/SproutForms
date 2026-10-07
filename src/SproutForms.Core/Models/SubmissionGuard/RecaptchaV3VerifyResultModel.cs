using System.Text.Json.Serialization;

namespace SproutForms.Core.Models.SubmissionGuard
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
