
namespace SproutForms.Core.Models.Outcomes
{
    public class RedirectUrlOutcomeType : IFormSubmitOutcomeType
    {
        public const string Alias = "redirect";

        string IFormSubmitOutcomeType.Alias => Alias;

        public Type ConfigurationType => typeof(RedirectUrlOutcomeConfig);

        public object GetDefaultConfiguration()
        {
            return new RedirectUrlOutcomeConfig();
        }

        public OutcomeResult Handle(object configuration)
        {
            var config = (RedirectUrlOutcomeConfig) configuration;
            return new OutcomeResult
            {
                OutcomeTypeAlias = Alias,
                Data = new Dictionary<string, object?>
                {
                    ["url"] = config.RedirectUrl
                }
            };
        }
    }
}
