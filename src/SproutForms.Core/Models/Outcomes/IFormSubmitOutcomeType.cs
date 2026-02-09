using System.Text.Json.Serialization;

namespace SproutForms.Core.Models.Outcomes
{
    public interface IFormSubmitOutcomeType
    {
        public string Alias { get; }
        public Type ConfigurationType { get; }

        public object GetDefaultConfiguration();
        public OutcomeResult Handle(object configuration);
    }
}
