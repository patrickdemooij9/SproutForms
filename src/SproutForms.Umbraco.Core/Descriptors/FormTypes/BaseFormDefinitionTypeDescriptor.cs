namespace SproutForms.Umbraco.Core.Descriptors.FormTypes
{
    public abstract class BaseFormDefinitionTypeDescriptor<TSettings> : BaseConfigDescriptor<TSettings>, IFormDefinitionTypeDescriptor where TSettings : class, new()
    {
        private readonly List<IFormFieldExtensionDescriptor> _fieldExtensions = [];

        public abstract string FormTypeAlias { get; }

        public abstract string DisplayName { get; }
        public abstract string Description { get; }

        public IReadOnlyCollection<IFormFieldExtensionDescriptor> FieldExtensions => _fieldExtensions;

        /// <summary>
        /// Describes the properties of the settings the form type adds to a field type, shown on a tab of the field named after the form type.
        /// </summary>
        protected void ExtendField<TFieldSettings>(string fieldTypeAlias, Action<FormFieldExtensionDescriptor<TFieldSettings>> describe) where TFieldSettings : class, new()
        {
            var descriptor = new FormFieldExtensionDescriptor<TFieldSettings>(fieldTypeAlias);
            describe(descriptor);
            _fieldExtensions.Add(descriptor);
        }
    }
}
