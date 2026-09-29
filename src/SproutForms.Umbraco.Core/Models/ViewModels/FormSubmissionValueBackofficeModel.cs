namespace SproutForms.Umbraco.Core.Models.ViewModels
{
    public class FormSubmissionValueBackofficeModel
    {
        public required string FieldTypeAlias { get; set; }
        public required string Name { get; set; }
        public required string Value { get; set; }

        // For a field group such as a repeater: the values of each entry, and the entry's title. Null for any other field
        public List<FormSubmissionEntryBackofficeModel>? Entries { get; set; }
    }
}
