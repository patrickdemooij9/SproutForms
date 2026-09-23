namespace SproutForms.Umbraco.Core.Models.ViewModels
{
    /// <summary>
    /// The properties a form type adds to a field type, with their default values.
    /// </summary>
    public class FormFieldExtensionBackofficeModel
    {
        public required string FieldTypeAlias { get; set; }
        public required FormPropertyBackofficeModel[] Properties { get; set; }
    }
}
