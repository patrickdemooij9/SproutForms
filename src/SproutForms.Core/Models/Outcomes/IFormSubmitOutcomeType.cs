using System.Text.Json.Serialization;

namespace SproutForms.Core.Models.Outcomes
{
    public interface IFormSubmitOutcomeType
    {
        public string Alias { get; }
        public string DisplayName { get; }
        public Type ConfigurationType { get; }

        public object GetDefaultConfiguration();
    }
}
