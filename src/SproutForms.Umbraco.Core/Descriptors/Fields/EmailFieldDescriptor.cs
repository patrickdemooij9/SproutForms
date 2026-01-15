using SproutForms.Core.Fields.Configs;

namespace SproutForms.Umbraco.Core.Descriptors.Fields
{
    public class EmailFieldDescriptor : BaseFieldDescriptor<EmailFieldConfig>
    {
        public override string FieldTypeAlias => "email";

        public override string DisplayName => "Email";

        public override string Icon => "icon-message";

        public EmailFieldDescriptor()
        {
            DefineMap(it => it.Placeholder, "placeholder", "Placeholder", "Umb.PropertyEditorUi.TextBox");
        }
    }
}
