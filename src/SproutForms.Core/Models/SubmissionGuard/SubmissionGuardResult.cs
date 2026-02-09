namespace SproutForms.Core.Models.SubmissionGuard
{
    public class SubmissionGuardResult
    {
        public bool Allowed { get; set; }
        public string? ErrorMessage { get; set; } = null;
    }
}
