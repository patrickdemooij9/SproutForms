using SproutForms.Core.Fields.Configs;

namespace SproutForms.Umbraco.Core.Descriptors.Fields
{
    public class HiddenFieldDescriptor : BaseFieldDescriptor<HiddenFieldConfig>
    {
        public override string FieldTypeAlias => "hidden";

        public override string DisplayName => "Hidden";

        public override string Icon => "icon-key";

        public HiddenFieldDescriptor()
        {
            DefineMap(it => it.DefaultValue, "defaultValue", "Default value", "Umb.PropertyEditorUi.TextBox");
            DefineMap(it => it.AllowOverrideFromClient, "allowOverrideFromClient", "Allow override from client", "Umb.PropertyEditorUi.Toggle");
        }
    }
}
