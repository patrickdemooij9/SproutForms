
namespace SproutForms.Core.Models.Outcomes
{
    public class RedirectUrlOutcomeType : IFormSubmitOutcomeType
    {
        public const string Alias = "redirect";

        string IFormSubmitOutcomeType.Alias => Alias;

        public string DisplayName => "Redirect";

        public Type ConfigurationType => typeof(RedirectUrlOutcomeConfig);

        public object GetDefaultConfiguration()
        {
            return new RedirectUrlOutcomeConfig();
        }
    }
}
