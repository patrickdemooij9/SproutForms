
namespace SproutForms.Core.Models.Outcomes
{
    public class ShowMessageOutcome : IFormSubmitOutcomeType
    {
        public const string Alias = "message";

        string IFormSubmitOutcomeType.Alias => Alias;

        public string DisplayName => "Show message";

        public Type ConfigurationType => typeof(ShowMessageOutcomeConfig);

        public object GetDefaultConfiguration()
        {
            return new ShowMessageOutcomeConfig
            {
                Message = "Thank you for your submission!"
            };
        }
    }
}
