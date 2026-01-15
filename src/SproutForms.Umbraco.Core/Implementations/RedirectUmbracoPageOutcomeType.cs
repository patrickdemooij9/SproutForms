using SproutForms.Core.Models.Outcomes;

namespace SproutForms.Umbraco.Core.Implementations
{
    public class RedirectUmbracoPageOutcomeType : IFormSubmitOutcomeType
    {
        public string Alias => "redirectUmbracoPage";

        public string DisplayName => "Redirect to Umbraco page";

        public Type ConfigurationType => typeof(RedirectUmbracoPageOutcomeConfig);

        public object GetDefaultConfiguration()
        {
            return new RedirectUmbracoPageOutcomeConfig();
        }
    }
}
