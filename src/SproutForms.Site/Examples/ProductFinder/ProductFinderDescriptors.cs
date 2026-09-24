using SproutForms.Umbraco.Core.Descriptors.FormTypes;
using SproutForms.Umbraco.Core.Descriptors.Outcomes;
using SproutForms.Umbraco.Core.Models.ViewModels;
using System.Text.Json;

namespace SproutForms.Site.Examples.ProductFinder
{
    /// <summary>
    /// How the product finder appears in the backoffice. Both lists use the key/value editor the radio options use, so the
    /// mappings convert between the lists and that editor's JSON.
    /// </summary>
    public class ProductFinderFormTypeDescriptor : BaseFormDefinitionTypeDescriptor<ProductFinderSettings>
    {
        public override string FormTypeAlias => ProductFinderFormType.TypeAlias;

        public override string DisplayName => "Product finder";
        public override string Description => "Questions whose answers point to products. Visitors are sent to the best match.";

        public ProductFinderFormTypeDescriptor()
        {
            DefineMap(it => it.Products, "products", "Products (name and URL)", "SproutForms.KeyValuePair",
                products => ToKeyValueJson(((IEnumerable<ProductFinderProduct>)products).Select(it => (it.Name, it.Url))),
                value => FromKeyValueJson(value).Select(it => new ProductFinderProduct { Name = it.Key, Url = it.Value }).ToList());

            ExtendField<ProductFinderAnswerSettings>("radio", field => field
                .Map(it => it.Recommendations, "recommendations", "Recommended product per answer (answer value and product name)", "SproutForms.KeyValuePair",
                    recommendations => ToKeyValueJson(((IEnumerable<ProductFinderRecommendation>)recommendations).Select(it => (it.Answer, it.Product))),
                    value => FromKeyValueJson(value).Select(it => new ProductFinderRecommendation { Answer = it.Key, Product = it.Value }).ToList()));
        }

        private static string ToKeyValueJson(IEnumerable<(string Key, string Value)> items)
            => JsonSerializer.Serialize(items.Select(it => new KeyValuePairModel { Key = it.Key, Value = it.Value }));

        // Rows left empty in the editor are dropped
        private static IEnumerable<KeyValuePairModel> FromKeyValueJson(object value)
            => (JsonSerializer.Deserialize<KeyValuePairModel[]>(value.ToString()!) ?? [])
                .Where(it => !string.IsNullOrWhiteSpace(it.Key) && !string.IsNullOrWhiteSpace(it.Value));
    }

    public class ProductRecommendationOutcomeDescriptor : BaseOutcomeDescriptor<ProductRecommendationOutcomeConfig>
    {
        public override string OutcomeTypeAlias => ProductRecommendationOutcomeType.TypeAlias;

        public override string DisplayName => "Go to the recommended product";
        public override string Description => "Sends the visitor to the product their answers point to.";

        public ProductRecommendationOutcomeDescriptor()
        {
            DefineMap(it => it.NoMatchMessage, "noMatchMessage", "Message when nothing matches", "Umb.PropertyEditorUi.TextBox");
        }
    }
}
