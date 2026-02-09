namespace SproutForms.Core.Models.SubmissionGuard
{
    public interface IFormSubmissionGuard
    {
        string Alias { get; }

        Task<SubmissionGuardResult> EvaluateAsync(Dictionary<string, string> postedValues);
        object? GetFrontendSettings();
    }
}
