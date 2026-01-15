using SproutForms.Umbraco.Core.Models.Attributes;

namespace SproutForms.Core.Fields.Configs
{
    public class TextFieldConfig
    {
        [BackofficeField("Umb.PropertyEditorUi.TextBox")]
        public string? Placeholder { get; set; }

        [BackofficeField("Umb.PropertyEditorUi.Integer")]
        public int? MinLength { get; set; }

        [BackofficeField("Umb.PropertyEditorUi.Integer")]
        public int? MaxLength { get; set; }

        [BackofficeField("Umb.PropertyEditorUi.TextBox")]
        public string? Regex { get; set; }
    }
}
