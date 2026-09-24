using SproutForms.Core.Models.Outcomes;

namespace SproutForms.Core.Models.FormTypes
{
    public abstract class FormDefinitionTypeBase<TSettings> : IFormDefinitionType where TSettings : class, new()
    {
        private readonly List<IFormFieldExtension> _fieldExtensions = [];

        public abstract string Alias { get; }

        public Type SettingsType => typeof(TSettings);

        public IReadOnlyCollection<IFormFieldExtension> FieldExtensions => _fieldExtensions;

        public virtual object GetDefaultSettings() => new TSettings();

        public virtual bool AllowsFieldType(IFormFieldType fieldType) => true;

        public virtual bool AllowsOutcomeType(IFormSubmitOutcomeType outcomeType) => true;

        public virtual Task<FormTypeSubmissionResult> ProcessSubmissionAsync(FormTypeSubmissionContext context, CancellationToken cancellationToken)
            => Task.FromResult(FormTypeSubmissionResult.None);

        /// <summary>
        /// The form's settings for this type, typed.
        /// </summary>
        protected static TSettings GetSettings(FormDefinition definition)
            => definition.Type.Settings as TSettings ?? new TSettings();

        /// <summary>
        /// A field's settings for the extension this type adds to its field type; defaults when the field has none.
        /// </summary>
        protected static TFieldSettings GetFieldSettings<TFieldSettings>(FormField field) where TFieldSettings : class, new()
            => field.Extension?.Settings as TFieldSettings ?? new TFieldSettings();

        /// <summary>
        /// Adds settings of <typeparamref name="TFieldSettings"/> to every field of the given field type in a form of this type.
        /// </summary>
        protected void ExtendField<TFieldSettings>(string fieldTypeAlias) where TFieldSettings : class, new()
        {
            if (_fieldExtensions.Any(it => it.FieldTypeAlias == fieldTypeAlias))
                throw new InvalidOperationException($"Form type '{Alias}' already extends field type '{fieldTypeAlias}'.");

            _fieldExtensions.Add(new FormFieldExtension<TFieldSettings>(fieldTypeAlias));
        }
    }
}
