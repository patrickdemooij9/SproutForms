using SproutForms.Core.Fields.Configs;

namespace SproutForms.Umbraco.Core.Descriptors.Fields
{
    public class CheckboxFieldDescriptor : BaseFieldDescriptor<CheckboxFieldConfig>
    {
        public override string FieldTypeAlias => "checkbox";

        public override string DisplayName => "Checkbox";

        public override string Icon => "icon-check";
    }
}
