using SproutForms.Core.Models.Outcomes;
using Umbraco.Cms.Core.Web;
using Umbraco.Extensions;

namespace SproutForms.Umbraco.Core.Implementations
{
    public class RedirectUmbracoPageOutcomeType : IFormSubmitOutcomeType
    {
        private readonly IUmbracoContextFactory _umbracoContextFactory;

        public string Alias => "redirectUmbracoPage";

        public Type ConfigurationType => typeof(RedirectUmbracoPageOutcomeConfig);

        public RedirectUmbracoPageOutcomeType(IUmbracoContextFactory umbracoContextFactory)
        {
            _umbracoContextFactory = umbracoContextFactory;
        }

        public object GetDefaultConfiguration()
        {
            return new RedirectUmbracoPageOutcomeConfig();
        }

        public OutcomeResult Handle(object configuration)
        {
            var config = (RedirectUmbracoPageOutcomeConfig)configuration;
            using var ctx = _umbracoContextFactory.EnsureUmbracoContext();
            return new OutcomeResult
            {
                OutcomeTypeAlias = Alias,
                Data = new Dictionary<string, object?>
                {
                    ["url"] = ctx.UmbracoContext.Content.GetById(config.NodeKey!.Value)?.Url()
                }
            };
        }
    }
}
