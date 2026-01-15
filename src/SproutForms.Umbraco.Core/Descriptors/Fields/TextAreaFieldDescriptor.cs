using SproutForms.Core.Fields.Configs;
using SproutForms.Umbraco.Core.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;

namespace SproutForms.Umbraco.Core.Descriptors.Fields
{
    public class TextAreaFieldDescriptor : BaseFieldDescriptor<TextAreaConfig>
    {
        public override string FieldTypeAlias => "textarea";

        public override string DisplayName => "Long question";

        public override string Icon => "icon-shape-rectangle-horizontal";

        public TextAreaFieldDescriptor()
        {
            DefineMap(it => it.Rows, "rows", "Rows", "Umb.PropertyEditorUi.Integer");
            DefineMap(it => it.MaxLength, "maxLength", "Max length", "Umb.PropertyEditorUi.Integer");
        }
    }
}
