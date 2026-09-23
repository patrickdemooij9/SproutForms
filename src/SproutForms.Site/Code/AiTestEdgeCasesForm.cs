using SproutForms.Core.Builders;
using SproutForms.Core.Models;

namespace SproutForms.Site.Code
{
    /// <summary>
    /// Regression form for the verify-in-site flow: awkward regex patterns and an upload field whose storage provider doesn't exist.
    /// </summary>
    public class AiTestEdgeCasesForm : ICodeFirstForm
    {
        public string Alias => "aiTestEdgeCases";

        public FormDefinition Build()
        {
            return new FormBuilder("aiTestEdgeCases", "AI test: edge cases")
                .Row(row => row
                    .Col(12, col => col
                        .Text("quoted", "Regex with a quote in it")
                            .Set(c => c.Regex = "^[^\"<>]*$")
                            .Done()
                    )
                )
                .Row(row => row
                    .Col(12, col => col
                        .Text("backtracking", "Regex that backtracks badly")
                            .Set(c => c.Regex = "^(a+)+$")
                            .Done()
                    )
                )
                .Row(row => row
                    .Col(12, col => col
                        .File("brokenUpload", "Upload with a missing storage provider")
                            .Set(c => c.StorageProviderAlias = "missing")
                            .Done()
                    )
                )
                .ThankYouMessage("Thank you!")
                .Build();
        }
    }
}
