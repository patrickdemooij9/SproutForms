
namespace SproutForms.Core.Models.Outcomes
{
    public class ShowMessageOutcome : IFormSubmitOutcomeType
    {
        public const string Alias = "message";

        string IFormSubmitOutcomeType.Alias => Alias;

        public Type ConfigurationType => typeof(ShowMessageOutcomeConfig);

        public object GetDefaultConfiguration()
        {
            return new ShowMessageOutcomeConfig
            {
                Message = "Thank you for your submission!"
            };
        }

        public OutcomeResult Handle(object configuration)
        {
            var config = (ShowMessageOutcomeConfig) configuration;
            return new OutcomeResult
            {
                OutcomeTypeAlias = Alias,
                Data = new Dictionary<string, object?>
                {
                    ["message"] = config.Message
                }
            };
        }
    }
}
