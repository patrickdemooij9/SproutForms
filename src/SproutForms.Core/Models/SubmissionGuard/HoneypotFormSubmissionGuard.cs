namespace SproutForms.Core.Models.SubmissionGuard
{
    public class HoneypotFormSubmissionGuard : IFormSubmissionGuard
    {
        internal const string HoneypotFieldName = "sf_Honeypot";

        public string Alias => "honeypot";

        public Task<SubmissionGuardResult> EvaluateAsync(Dictionary<string, string> postedValues)
        {
            if (postedValues.TryGetValue(HoneypotFieldName, out var honeypotValue)
                && !string.IsNullOrWhiteSpace(honeypotValue))
            {
                return Task.FromResult(new SubmissionGuardResult
                {
                    Allowed = false,
                    ErrorMessage = "Submission rejected"
                });
            }

            return Task.FromResult(new SubmissionGuardResult { Allowed = true });
        }

        public object? GetFrontendSettings()
        {
            return null;
        }

        public IEnumerable<GuardFormField> GetFormFields()
        {
            yield return new GuardFormField
            {
                Name = HoneypotFieldName,
                Type = "text",
                Id = HoneypotFieldName,
                Label = "Leave this field empty",
                Value = "",
                VisuallyHidden = true,
                AutoComplete = "off",
                TabIndex = -1
            };
        }
    }
}
