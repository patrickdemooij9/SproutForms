using SproutForms.Core.Models.Outcomes;

namespace SproutForms.Core.Models.FormTypes
{
    /// <summary>
    /// The kind of form a definition is, such as a standard form, a quiz or a poll. It decides which field and outcome types the form may use,
    /// and which settings it adds to the form and to its fields.
    /// </summary>
    public interface IFormDefinitionType
    {
        string Alias { get; }
        Type SettingsType { get; }

        object GetDefaultSettings();

        bool AllowsFieldType(IFormFieldType fieldType);
        bool AllowsOutcomeType(IFormSubmitOutcomeType outcomeType);

        IReadOnlyCollection<IFormFieldExtension> FieldExtensions { get; }
    }
}
