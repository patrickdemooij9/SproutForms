namespace SproutForms.Core.Models.ClientModels
{
    /// <summary>
    /// The active submission guard, with what a front-end needs to satisfy it, such as a reCAPTCHA site key or the honeypot's field name.
    /// </summary>
    public sealed class FormClientSubmissionGuard
    {
        public required string Alias { get; init; }
        public object? Settings { get; init; }
    }
}
