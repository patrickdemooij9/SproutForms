namespace SproutForms.Core.Models.Outcomes
{
    /// <summary>
    /// A saved submission the outcome responds to, with the outcome's configuration on the form.
    /// </summary>
    public sealed class FormSubmitOutcomeContext
    {
        public required object Configuration { get; init; }

        // Holds the submitted values and the results the form's type computed, such as a quiz score
        public required FormSubmission Submission { get; init; }
        public required FormVersion Version { get; init; }
    }
}
