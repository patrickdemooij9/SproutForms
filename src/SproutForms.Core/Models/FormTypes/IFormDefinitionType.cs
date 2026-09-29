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

        /// <summary>
        /// Runs after field validation, before the submission is saved. Returns results to store with the submission, such as a
        /// quiz score, or errors that reject it.
        /// </summary>
        Task<FormTypeSubmissionResult> ProcessSubmissionAsync(FormTypeSubmissionContext context, CancellationToken cancellationToken);

        /// <summary>
        /// The form settings of this type a front-end may see. Nothing by default, because they can hold what the visitor mustn't know.
        /// </summary>
        object? GetClientSettings(FormDefinition definition) => null;

        /// <summary>
        /// The extension settings of a field a front-end may see. Nothing by default: they can hold secrets such as a quiz's correct answer.
        /// </summary>
        object? GetClientFieldExtension(FormField field) => null;
    }
}
