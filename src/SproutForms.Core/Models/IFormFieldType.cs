using Newtonsoft.Json.Linq;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace SproutForms.Core.Models
{
    public interface IFormFieldType
    {
        string Alias { get; }
        Type ConfigurationType { get; }
        object DefaultConfiguration { get; }
        bool RendersOwnLabel { get; }

        ValidationResult Validate(JsonElement value, object configurationJson);
        IEnumerable<ValidationRule> GetValidationRules(object configuration);
    }
}
