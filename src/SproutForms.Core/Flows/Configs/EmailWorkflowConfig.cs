using SproutForms.Umbraco.Core.Models.Attributes;

namespace SproutForms.Core.Flows.Configs
{
    public class EmailWorkflowConfig
    {
        [BackofficeField("Umb.PropertyEditorUi.TextBox")]
        public string To { get; set; }

        [BackofficeField("Umb.PropertyEditorUi.TextBox")]
        public string From { get; set; }

        [BackofficeField("Umb.PropertyEditorUi.TextBox")]
        public string Subject { get; set; }
    }
}
