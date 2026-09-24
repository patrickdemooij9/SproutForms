using SproutForms.Core.Builders;
using SproutForms.Core.Fields.Configs;
using SproutForms.Core.Models;

namespace SproutForms.Site.Examples.ProductFinder
{
    /// <summary>
    /// A product finder built in code. The same can be built in the backoffice: Create → Product finder.
    /// </summary>
    public class ExampleProductFinderForm : ICodeFirstForm
    {
        public string Alias => "exampleProductFinder";

        public FormDefinition Build()
        {
            return new FormBuilder(Alias, "Example: find your coffee machine")
                .OfType(ProductFinderFormType.TypeAlias, new ProductFinderSettings
                {
                    Products =
                    [
                        new() { Name = "Espresso machine", Url = "/products/espresso-machine" },
                        new() { Name = "Filter machine", Url = "/products/filter-machine" }
                    ]
                })
                .Row(row => row
                    .Col(12, col => col
                        .Radio("cups", "How many cups a day?")
                            .Set(c => c.Options =
                            [
                                new RadioFieldOption { Label = "One or two", Value = "few" },
                                new RadioFieldOption { Label = "A whole pot", Value = "many" }
                            ])
                            .Extend(new ProductFinderAnswerSettings
                            {
                                Recommendations =
                                [
                                    new() { Answer = "few", Product = "Espresso machine" },
                                    new() { Answer = "many", Product = "Filter machine" }
                                ]
                            })
                            .Required()
                            .Done()
                    )
                )
                .Row(row => row
                    .Col(12, col => col
                        .Radio("taste", "What do you like?")
                            .Set(c => c.Options =
                            [
                                new RadioFieldOption { Label = "Strong and short", Value = "strong" },
                                new RadioFieldOption { Label = "Mild and long", Value = "mild" }
                            ])
                            .Extend(new ProductFinderAnswerSettings
                            {
                                Recommendations =
                                [
                                    new() { Answer = "strong", Product = "Espresso machine" },
                                    new() { Answer = "mild", Product = "Filter machine" }
                                ]
                            })
                            .Required()
                            .Done()
                    )
                )
                .SetOutcome(ProductRecommendationOutcomeType.TypeAlias, new ProductRecommendationOutcomeConfig())
                .Build();
        }
    }
}
