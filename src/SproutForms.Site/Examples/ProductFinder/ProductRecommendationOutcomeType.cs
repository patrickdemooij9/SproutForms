using SproutForms.Core.Models.FormTypes;
using SproutForms.Core.Models.Outcomes;

namespace SproutForms.Site.Examples.ProductFinder
{
    /// <summary>
    /// Example outcome: sends the visitor to the product <see cref="ProductFinderFormType"/> recommended. A "url" also redirects
    /// without JavaScript. When no answer pointed to a product, the visitor gets the configured message instead.
    /// </summary>
    public class ProductRecommendationOutcomeType : IFormSubmitOutcomeType, IRestrictedToFormTypes
    {
        public const string TypeAlias = "productRecommendation";

        public string Alias => TypeAlias;

        public Type ConfigurationType => typeof(ProductRecommendationOutcomeConfig);

        public IReadOnlyCollection<string> FormTypeAliases => [ProductFinderFormType.TypeAlias];

        public object GetDefaultConfiguration() => new ProductRecommendationOutcomeConfig();

        public Task<OutcomeResult> HandleAsync(FormSubmitOutcomeContext context, CancellationToken cancellationToken)
        {
            var config = (ProductRecommendationOutcomeConfig)context.Configuration;
            var results = context.Submission.Results;
            var product = results.TryGetValue("product", out var name) ? name.GetString() : null;
            var url = results.TryGetValue("productUrl", out var productUrl) ? productUrl.GetString() : null;

            return Task.FromResult(new OutcomeResult
            {
                Data = product is null || string.IsNullOrWhiteSpace(url)
                    ? new Dictionary<string, object?> { ["message"] = config.NoMatchMessage }
                    : new Dictionary<string, object?> { ["product"] = product, ["url"] = url }
            });
        }
    }

    public class ProductRecommendationOutcomeConfig
    {
        public string NoMatchMessage { get; set; } = "We couldn't find a match. Have a look at all our products instead.";
    }
}
