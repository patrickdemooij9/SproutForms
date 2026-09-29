namespace SproutForms.Umbraco.Core.Models.ViewModels
{
    public class FormSubmissionEntryBackofficeModel
    {
        public required string Title { get; set; }
        public required FormSubmissionValueBackofficeModel[] Values { get; set; }
    }
}
