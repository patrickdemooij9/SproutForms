using SproutForms.Core.Models.SubmissionGuard;

namespace SproutForms.Umbraco.Core.Implementations
{
    public class HoneypotSubmissionGuard : IFormSubmissionGuard
    {
        public const string FieldName = "sf_Honeypot";

        public string Alias => "honeypot";

        public string? PartialViewPath => "~/Views/Forms/Guards/Honeypot.cshtml";

        public Task<SubmissionGuardResult> EvaluateAsync(Dictionary<string, string> postedValues)
        {
            // The field is hidden from visitors, so only a bot filling in every input gives it a value
            if (postedValues.TryGetValue(FieldName, out var value) && !string.IsNullOrEmpty(value))
                return Task.FromResult(new SubmissionGuardResult { Allowed = false, ErrorMessage = "Your submission could not be processed." });

            return Task.FromResult(new SubmissionGuardResult { Allowed = true });
        }

        public object? GetFrontendSettings()
        {
            return null;
        }
    }
}
