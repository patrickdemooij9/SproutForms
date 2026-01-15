using Newtonsoft.Json.Linq;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace SproutForms.Core.Models
{
    public interface IFormFieldType
    {
        Guid Id { get; } //TODO: Prop replace this with Alias
        string Alias { get; }
        string DisplayName { get; }
        Type ConfigurationType { get; }
        object DefaultConfiguration { get; }
        bool RendersOwnLabel { get; }

        ValidationResult Validate(JsonElement value, object configurationJson);
    }
}
