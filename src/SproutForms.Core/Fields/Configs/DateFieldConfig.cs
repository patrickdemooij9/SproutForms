using SproutForms.Umbraco.Core.Models.Attributes;

namespace SproutForms.Core.Fields.Configs
{
    public class DateFieldConfig
    {
        [BackofficeField("Umb.PropertyEditorUi.Date")]
        public DateTime? Min { get; set; }

        [BackofficeField("Umb.PropertyEditorUi.Date")]
        public DateTime? Max { get; set; }

        [BackofficeField("Umb.PropertyEditorUi.Toggle")]
        public bool IncludeTime { get; set; } = false;
    }
}
