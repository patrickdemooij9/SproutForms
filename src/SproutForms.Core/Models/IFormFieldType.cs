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

        /// <summary>
        /// The part of a field's configuration a front-end may see, such as its placeholder or options. It is sent to the browser,
        /// so leave out settings only the server needs. Razor views still get the whole configuration.
        /// </summary>
        object? GetClientConfiguration(object configuration) => configuration;
    }
}
