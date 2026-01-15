using SproutForms.Umbraco.Core.Models.Attributes;

namespace SproutForms.Core.Fields.Configs
{
    public class HiddenFieldConfig
    {
        [BackofficeField("Umb.PropertyEditorUi.TextBox")]
        public string? DefaultValue { get; set; }

        [BackofficeField("Umb.PropertyEditorUi.Toggle")]
        public bool AllowOverrideFromClient { get; set; } = false;
    }
}
