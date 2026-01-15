using SproutForms.Umbraco.Core.Models.Attributes;

namespace SproutForms.Core.Fields.Configs
{
    public class TextAreaConfig
    {
        [BackofficeField("Umb.PropertyEditorUi.Integer")]
        public int Rows { get; init; } = 5;

        [BackofficeField("Umb.PropertyEditorUi.Integer")]
        public int? MaxLength { get; init; }
    }
}
