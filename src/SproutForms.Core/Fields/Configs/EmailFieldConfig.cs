using SproutForms.Umbraco.Core.Models.Attributes;

namespace SproutForms.Core.Fields.Configs
{
    public class EmailFieldConfig
    {
        [BackofficeField("Umb.PropertyEditorUi.TextBox")]
        public string? Placeholder { get; set; }
    }
}
