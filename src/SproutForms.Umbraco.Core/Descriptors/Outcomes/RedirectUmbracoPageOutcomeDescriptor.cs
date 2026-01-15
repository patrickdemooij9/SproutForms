using SproutForms.Umbraco.Core.Implementations;

namespace SproutForms.Umbraco.Core.Descriptors.Outcomes
{
    public class RedirectUmbracoPageOutcomeDescriptor : BaseOutcomeDescriptor<RedirectUmbracoPageOutcomeConfig>
    {
        public override string OutcomeTypeAlias => "redirectUmbracoPage";

        public override string DisplayName => "Redirect to Umbraco Page";

        public override string Description => "Redirect the user to a specific Umbraco page upon form submission.";

        public RedirectUmbracoPageOutcomeDescriptor()
        {
            DefineMap(it => it.NodeKey, "nodeKey", "Page", "Umb.PropertyEditorUi.DocumentPicker");
        }
    }
}
