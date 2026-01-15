using SproutForms.Umbraco.Core.Models.Attributes;

namespace SproutForms.Core.Models.Outcomes
{
    public class RedirectUrlOutcomeConfig
    {
        [BackofficeField("Umb.PropertyEditorUi.TextBox")]
        public string RedirectUrl { get; set; }
    }
}
