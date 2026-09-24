using SproutForms.Umbraco.Core.Models.ViewModels;
using System.Linq.Expressions;

namespace SproutForms.Umbraco.Core.Descriptors.FormTypes
{
    public interface IFormFieldExtensionDescriptor
    {
        string FieldTypeAlias { get; }

        FormPropertyBackofficeModel[] FromConfig(object configuration);
        object ToConfig(Dictionary<string, object?> properties);
    }

    /// <summary>
    /// Describes the backoffice properties of the settings a form type adds to a field type.
    /// </summary>
    public class FormFieldExtensionDescriptor<TSettings> : BaseConfigDescriptor<TSettings>, IFormFieldExtensionDescriptor where TSettings : class, new()
    {
        public string FieldTypeAlias { get; }

        public FormFieldExtensionDescriptor(string fieldTypeAlias)
        {
            FieldTypeAlias = fieldTypeAlias;
        }

        public FormFieldExtensionDescriptor<TSettings> Map(Expression<Func<TSettings, object?>> expression, string alias, string displayName, string propertyTypeAlias, Func<object, object>? overrideFromConfig = null, Func<object, object>? overrideToConfig = null)
        {
            DefineMap(expression, alias, displayName, propertyTypeAlias, overrideFromConfig, overrideToConfig);
            return this;
        }
    }
}
