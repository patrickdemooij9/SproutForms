namespace SproutForms.Core.Models.SubmissionGuard
{
    public interface IFormSubmissionGuard
    {
        string Alias { get; }

        Task<SubmissionGuardResult> EvaluateAsync(Dictionary<string, string> postedValues);
        object? GetFrontendSettings();

        /// <summary>
        /// A partial view rendered inside every form, for a guard that needs markup of its own.
        /// </summary>
        string? PartialViewPath => null;
    }
}
