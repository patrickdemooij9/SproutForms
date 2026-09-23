using SproutForms.Umbraco.Core.Descriptors.FormTypes;
using SproutForms.Umbraco.Core.Descriptors.Outcomes;

namespace SproutForms.Site.Examples.Poll
{
    public class PollFormTypeDescriptor : BaseFormDefinitionTypeDescriptor<PollSettings>
    {
        public override string FormTypeAlias => PollFormType.TypeAlias;

        public override string DisplayName => "Poll";
        public override string Description => "Radio button questions. After voting, visitors see how everyone voted.";
    }

    public class PollResultsOutcomeDescriptor : BaseOutcomeDescriptor<PollResultsOutcomeConfig>
    {
        public override string OutcomeTypeAlias => PollResultsOutcomeType.TypeAlias;

        public override string DisplayName => "Show poll results";
        public override string Description => "Shows the votes per option, including the visitor's own.";
    }
}
