using SproutForms.Core.Fields;
using SproutForms.Core.Models;
using SproutForms.Core.Models.FormTypes;

namespace SproutForms.Site.Examples.Poll
{
    /// <summary>
    /// Example form type: a poll. It only allows radio buttons, so every question has votes to count. It needs no settings and
    /// doesn't process submissions; <see cref="PollResultsOutcomeType"/> counts the votes.
    /// </summary>
    public class PollFormType : FormDefinitionTypeBase<PollSettings>
    {
        public const string TypeAlias = "poll";

        public override string Alias => TypeAlias;

        public override bool AllowsFieldType(IFormFieldType fieldType) => fieldType is RadioFieldType;
    }

    public class PollSettings
    {
    }
}
