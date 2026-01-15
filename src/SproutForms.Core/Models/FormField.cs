using SproutForms.Core.Models.Conditions;
using System.Text.Json;

namespace SproutForms.Core.Models
{
    public class FormField
    {
        public required string Alias { get; set; }
        public required string Label { get; set; }
        public Guid FieldTypeId { get; set; }
        public bool Required { get; set; }
        public required object Configuration { get; set; }
        public FieldConditions? Conditions { get; set; }
    }
}
