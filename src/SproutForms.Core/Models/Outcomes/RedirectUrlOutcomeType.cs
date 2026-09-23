
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

        public Task<OutcomeResult> HandleAsync(FormSubmitOutcomeContext context, CancellationToken cancellationToken)
        {
            var config = (RedirectUrlOutcomeConfig) context.Configuration;
            return Task.FromResult(new OutcomeResult
            {
                OutcomeTypeAlias = Alias,
                Data = new Dictionary<string, object?>
                {
                    ["url"] = config.RedirectUrl
                }
            });
        }
    }
}
