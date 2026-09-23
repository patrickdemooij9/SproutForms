using SproutForms.Umbraco.Core.Models.ViewModels;

namespace SproutForms.Umbraco.Core.Descriptors.FormTypes
{
    public interface IFormDefinitionTypeDescriptor
    {
        string FormTypeAlias { get; }

        string DisplayName { get; }
        string Description { get; }

        IReadOnlyCollection<IFormFieldExtensionDescriptor> FieldExtensions { get; }

        FormPropertyBackofficeModel[] FromConfig(object configuration);
        object ToConfig(Dictionary<string, object?> properties);
    }
}
