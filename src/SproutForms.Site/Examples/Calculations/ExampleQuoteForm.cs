using SproutForms.Core.Builders;
using SproutForms.Core.Fields.Configs;
using SproutForms.Core.Models;
using SproutForms.Core.Models.Outcomes;

namespace SproutForms.Site.Examples.Calculations
{
    /// <summary>
    /// A price quote: the bag size sets a price per bag, grinding adds a fee per bag, and the quantity multiplies it. The server works
    /// out the total, and the message shows it.
    /// </summary>
    public class ExampleQuoteForm : ICodeFirstForm
    {
        public string Alias => "exampleQuote";

        public FormDefinition Build()
        {
            return new FormBuilder(Alias, "Example: coffee beans quote")
                .Row(row => row
                    .Col(6, col => col
                        .Select("size", "Bag size")
                            .Set(c => c.Options =
                            [
                                new SelectFieldOption { Label = "250 g (7.50)", Value = "250" },
                                new SelectFieldOption { Label = "1 kg (24.95)", Value = "1000" }
                            ])
                            .Required()
                            .Done())
                    .Col(6, col => col.Text("quantity", "Number of bags").Required().Set(c => c.Regex = "^[1-9][0-9]?$").Done()))
                .Row(row => row.Col(12, col => col.Checkbox("ground", "Grind the beans (0.50 per bag)").Done()))
                .Variable("perBag", v => v.Number(decimals: 2))
                .Variable("total", v => v.Label("Total").Number(decimals: 2))
                .Calculate("perBag", rules => rules
                    .When(c => c.Field("size").Is("250")).Set(7.50m)
                    .When(c => c.Field("size").Is("1000")).Set(24.95m)
                    .When(c => c.Field("ground").Is("true")).Add(0.50m))
                .Calculate("total", rules => rules
                    .Always().Set(ValueOf.Variable("perBag"))
                    .Always().Multiply(ValueOf.Field("quantity")))
                .SetOutcome(ShowMessageOutcome.Alias, new ShowMessageOutcomeConfig { Message = "Thanks! Your quote comes to {var:total}." })
                .Build();
        }
    }
}
