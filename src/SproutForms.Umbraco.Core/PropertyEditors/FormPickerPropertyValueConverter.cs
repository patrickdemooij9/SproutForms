using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PropertyEditors;

namespace SproutForms.Umbraco.Core.PropertyEditors
{
    public class FormPickerPropertyValueConverter : PropertyValueConverterBase
    {
        public override bool IsConverter(IPublishedPropertyType propertyType)
            => propertyType.EditorUiAlias == "sproutForms.propertyEditors.formPicker";

        public override Type GetPropertyValueType(IPublishedPropertyType propertyType)
            => typeof(Guid?);

        public override object? ConvertIntermediateToObject(
            IPublishedElement owner,
            IPublishedPropertyType propertyType,
            PropertyCacheLevel referenceCacheLevel,
            object? inter,
            bool preview)
        {
            if (inter is null) return null;

            if (Guid.TryParse(inter.ToString(), out var result))
            {
                return result;
            }
            return null;
        }
    }
}
