using SproutForms.Core.Builders;
using SproutForms.Core.Fields.Configs;
using SproutForms.Core.Models;

namespace SproutForms.Site.Examples.Poll
{
    /// <summary>
    /// A poll built in code. The same can be built in the backoffice: Create → Poll.
    /// </summary>
    public class ExamplePollForm : ICodeFirstForm
    {
        public string Alias => "examplePoll";

        public FormDefinition Build()
        {
            return new FormBuilder(Alias, "Example: coffee poll")
                .OfType(PollFormType.TypeAlias, new PollSettings())
                .Row(row => row
                    .Col(12, col => col
                        .Radio("favourite", "How do you take your coffee?")
                            .Set(c => c.Options =
                            [
                                new RadioFieldOption { Label = "Black", Value = "black" },
                                new RadioFieldOption { Label = "With milk", Value = "milk" },
                                new RadioFieldOption { Label = "I prefer tea", Value = "tea" }
                            ])
                            .Required()
                            .Done()
                    )
                )
                .SetOutcome(PollResultsOutcomeType.TypeAlias, new PollResultsOutcomeConfig())
                .Build();
        }
    }
}
