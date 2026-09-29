using SproutForms.Core.Models.Outcomes;
using Umbraco.Cms.Core.Models.PublishedContent;
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

        public Task<OutcomeResult> HandleAsync(FormSubmitOutcomeContext context, CancellationToken cancellationToken)
        {
            var config = (RedirectUmbracoPageOutcomeConfig)context.Configuration;
            using var ctx = _umbracoContextFactory.EnsureUmbracoContext();
            var page = ctx.UmbracoContext.Content.GetById(config.NodeKey!.Value);
            return Task.FromResult(new OutcomeResult
            {
                OutcomeTypeAlias = Alias,
                Data = new Dictionary<string, object?>
                {
                    ["url"] = page?.Url(),
                    // A headless front-end has its own routing, so it gets the page itself too
                    ["contentKey"] = page?.Key,
                    ["path"] = page?.Url(mode: UrlMode.Relative)
                }
            });
        }
    }
}
