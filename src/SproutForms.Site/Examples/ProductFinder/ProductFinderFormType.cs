using SproutForms.Core.Fields;
using SproutForms.Core.Models;
using SproutForms.Core.Models.FormTypes;
using System.Text.Json;

namespace SproutForms.Site.Examples.ProductFinder
{
    /// <summary>
    /// Example form type: a product finder. The form lists the products it can recommend, and every answer of a radio question
    /// can point to one of them. The product most answers point to is the recommendation, stored with the submission;
    /// <see cref="ProductRecommendationOutcomeType"/> sends the visitor to it.
    /// </summary>
    public class ProductFinderFormType : FormDefinitionTypeBase<ProductFinderSettings>
    {
        public const string TypeAlias = "productFinder";

        public override string Alias => TypeAlias;

        public ProductFinderFormType()
        {
            ExtendField<ProductFinderAnswerSettings>("radio");
        }

        public override bool AllowsFieldType(IFormFieldType fieldType) => fieldType is not FileFieldType;

        public override Task<FormTypeSubmissionResult> ProcessSubmissionAsync(FormTypeSubmissionContext context, CancellationToken cancellationToken)
        {
            var settings = GetSettings(context.Definition);
            // Distinct: an editor can list the same product twice
            var votes = settings.Products.Select(product => product.Name).Distinct().ToDictionary(name => name, _ => 0);

            foreach (var field in context.Definition.Fields.Where(it => it.Extension != null))
            {
                if (!context.Values.TryGetValue(field.Alias, out var value) || value.ValueKind != JsonValueKind.String) continue;

                var answer = value.GetString();
                var recommendation = GetFieldSettings<ProductFinderAnswerSettings>(field).Recommendations
                    .FirstOrDefault(it => it.Answer == answer);
                if (recommendation != null && votes.ContainsKey(recommendation.Product))
                {
                    votes[recommendation.Product]++;
                }
            }

            // On a tie, the product listed first wins
            var best = settings.Products
                .Where(product => votes[product.Name] > 0)
                .OrderByDescending(product => votes[product.Name])
                .FirstOrDefault();

            return Task.FromResult(FormTypeSubmissionResult.WithResults(new Dictionary<string, object?>
            {
                ["product"] = best?.Name,
                ["productUrl"] = best?.Url
            }));
        }
    }

    public class ProductFinderSettings
    {
        public List<ProductFinderProduct> Products { get; set; } = [];
    }

    public class ProductFinderProduct
    {
        public required string Name { get; set; }
        public required string Url { get; set; }
    }

    public class ProductFinderAnswerSettings
    {
        public List<ProductFinderRecommendation> Recommendations { get; set; } = [];
    }

    public class ProductFinderRecommendation
    {
        // The value of the answer's option, and the name of the product it points to
        public required string Answer { get; set; }
        public required string Product { get; set; }
    }
}
