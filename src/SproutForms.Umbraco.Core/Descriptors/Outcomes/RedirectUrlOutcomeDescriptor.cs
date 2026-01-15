using SproutForms.Core.Models.Outcomes;

namespace SproutForms.Umbraco.Core.Descriptors.Outcomes
{
    public class RedirectUrlOutcomeDescriptor : BaseOutcomeDescriptor<RedirectUrlOutcomeConfig>
    {
        public override string OutcomeTypeAlias => "redirect";

        public override string DisplayName => "Redirect to URL";

        public override string Description => "Redirect the user to a specified URL upon form submission.";

        public RedirectUrlOutcomeDescriptor()
        {
            DefineMap(it => it.RedirectUrl, "redirectUrl", "Redirect Url", "Umb.PropertyEditorUi.TextBox");
        }
    }
}
