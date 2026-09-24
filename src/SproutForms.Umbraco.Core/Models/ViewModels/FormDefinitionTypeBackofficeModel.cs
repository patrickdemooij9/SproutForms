namespace SproutForms.Umbraco.Core.Models.ViewModels
{
    /// <summary>
    /// A registered form type, with the field and outcome types a form of that type may use.
    /// </summary>
    public class FormDefinitionTypeBackofficeModel
    {
        public required string Alias { get; set; }
        public required string DisplayName { get; set; }
        public required string Description { get; set; }
        public required FormPropertyBackofficeModel[] Properties { get; set; }
        public required string[] AllowedFieldTypeAliases { get; set; }
        public required string[] AllowedOutcomeTypeAliases { get; set; }
        public required FormFieldExtensionBackofficeModel[] FieldExtensions { get; set; }
    }
}
