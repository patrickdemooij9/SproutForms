using System.Text.Json.Serialization;

namespace SproutForms.Umbraco.Core.Implementations
{
    internal class RecaptchaV3VerifyResultModel
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }
    }
}
