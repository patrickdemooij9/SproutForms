namespace SproutForms.Umbraco.Core.Models.ViewModels
{
    /// <summary>
    /// A variable the form's calculations worked out for a submission, formatted with the decimals it keeps.
    /// </summary>
    public class FormSubmissionVariableBackofficeModel
    {
        public required string Alias { get; set; }
        public required string Name { get; set; }
        public required string Value { get; set; }
    }
}
