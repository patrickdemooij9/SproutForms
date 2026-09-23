using Microsoft.Extensions.Options;
using SproutForms.Core.Models.SubmissionGuard;
using System.Net.Http.Json;

namespace SproutForms.Umbraco.Core.Implementations
{
    public class RecaptchaV3SubmissionGuard : IFormSubmissionGuard
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly RecaptchaV3Options _config;

        public string Alias => "recaptchaV3";

        public RecaptchaV3SubmissionGuard(IHttpClientFactory httpClientFactory, IOptions<RecaptchaV3Options> options)
        {
            _httpClientFactory = httpClientFactory;
            _config = options.Value;
        }

        public async Task<SubmissionGuardResult> EvaluateAsync(Dictionary<string, string> postedValues)
        {
            if (!string.IsNullOrWhiteSpace(_config.SecretKey) && postedValues.ContainsKey("g-recaptcha-response"))
            {
                var parameters = new Dictionary<string, string>
            {
                {"secret", _config.SecretKey },
                {"response", postedValues["g-recaptcha-response"] }
            };

                using var content = new FormUrlEncodedContent(parameters);
                var client = _httpClientFactory.CreateClient();
                var response = await client.PostAsync("https://www.google.com/recaptcha/api/siteverify", content);

                if (!response.IsSuccessStatusCode)
                    return new SubmissionGuardResult() { Allowed = false, ErrorMessage = "Recaptcha failed" };
                else
                {
                    var responseContent = await response.Content.ReadFromJsonAsync<RecaptchaV3VerifyResultModel>();
                    // v3 returns success for any valid token, bots included; the score is what tells them apart
                    if (responseContent is null
                        || !responseContent.Success
                        || responseContent.Score < _config.MinimumScore
                        || !string.Equals(responseContent.Action, _config.Action, StringComparison.Ordinal))
                        return new SubmissionGuardResult() { Allowed = false, ErrorMessage = "Recaptcha failed" };
                    return new SubmissionGuardResult { Allowed = true };
                }
            }
            return new SubmissionGuardResult() { Allowed = false, ErrorMessage = "Recaptcha failed" };
        }

        public object GetFrontendSettings()
        {
            return new
            {
                SiteKey = _config.SiteKey,
                Action = _config.Action,
            };
        }
    }
}
