namespace SproutForms.Core.Models.FormTypes
{
    /// <summary>
    /// A field's settings for the extension its form type adds to the field's type.
    /// </summary>
    public class FormFieldExtensionValue
    {
        public required string FormTypeAlias { get; set; }
        public required object Settings { get; set; }
    }
}
