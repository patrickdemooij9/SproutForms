using Microsoft.Extensions.Options;
using SproutForms.Core.Models.SubmissionGuard;
using System.Net.Http.Json;

namespace SproutForms.Umbraco.Core.Implementations
{
    public class RecaptchaV3SubmissionGuard : IFormSubmissionGuard
    {
        private readonly HttpClient _httpClient;
        private readonly RecaptchaV3Options _config;

        public string Alias => "recaptchaV3";

        public RecaptchaV3SubmissionGuard(HttpClient httpClient, IOptions<RecaptchaV3Options> options)
        {
            _httpClient = httpClient;
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
                var response = await _httpClient.PostAsync("https://www.google.com/recaptcha/api/siteverify", content);

                if (!response.IsSuccessStatusCode)
                    return new SubmissionGuardResult() { Allowed = false, ErrorMessage = "Recaptcha failed" };
                else
                {
                    var responseContent = await response.Content.ReadFromJsonAsync<RecaptchaV3VerifyResultModel>();
                    if (responseContent is null || !responseContent.Success)
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
            };
        }
    }
}
