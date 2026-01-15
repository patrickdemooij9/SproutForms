using SproutForms.Core.Fields.Configs;
using SproutForms.Umbraco.Core.Models.ViewModels;

namespace SproutForms.Umbraco.Core.Descriptors.Fields
{
    public class TextFieldDescriptor : BaseFieldDescriptor<TextFieldConfig>
    {
        public override string FieldTypeAlias => "text";

        public override string DisplayName => "Small question";

        public override string Icon => "icon-shape-square";

        public TextFieldDescriptor()
        {
            DefineMap((it) => it.Placeholder, "placeholder", "Placeholder", "Umb.PropertyEditorUi.TextBox");
            DefineMap((it) => it.MinLength, "minLength", "Minimum length", "Umb.PropertyEditorUi.Integer");
            DefineMap((it) => it.MaxLength, "maxLength", "Maximum length", "Umb.PropertyEditorUi.Integer");
            DefineMap((it) => it.Regex, "regex", "Regex", "Umb.PropertyEditorUi.TextBox");
        }
    }
}
