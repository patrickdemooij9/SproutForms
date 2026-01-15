using SproutForms.Core.Models.Outcomes;
using System.Text.Json;

namespace SproutForms.Core.Models
{
    public class FormSubmitOutcome
    {
        public required string OutcomeTypeAlias { get; set; }
        public required object Configuration { get; set; }

        public static FormSubmitOutcome Default()
        {
            var submitOutcomeConfig = new ShowMessageOutcomeConfig { Message = "Thank you for your submission." };
            return new FormSubmitOutcome
            {
                OutcomeTypeAlias = "message",
                Configuration = submitOutcomeConfig
            };
        }
    }
}
