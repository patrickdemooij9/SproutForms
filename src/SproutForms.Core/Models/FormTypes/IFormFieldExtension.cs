namespace SproutForms.Core.Models.FormTypes
{
    /// <summary>
    /// Extra settings a form type adds to every field of one field type, such as the correct answer of a quiz question.
    /// </summary>
    public interface IFormFieldExtension
    {
        string FieldTypeAlias { get; }
        Type SettingsType { get; }

        object GetDefaultSettings();
    }

    public class FormFieldExtension<TSettings> : IFormFieldExtension where TSettings : class, new()
    {
        public string FieldTypeAlias { get; }

        public Type SettingsType => typeof(TSettings);

        public FormFieldExtension(string fieldTypeAlias)
        {
            FieldTypeAlias = fieldTypeAlias;
        }

        public object GetDefaultSettings() => new TSettings();
    }
}
