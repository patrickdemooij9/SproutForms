using SproutForms.Core.Models.Conditions;
using SproutForms.Core.Models.FormTypes;
using System.Text.Json;

namespace SproutForms.Core.Models
{
    public class FormField
    {
        public required string Alias { get; set; }
        public required string Label { get; set; }
        public required string FieldTypeAlias { get; set; }
        public bool Required { get; set; }
        public required object Configuration { get; set; }
        // When the field shows, hides or is required; a rule that changes a variable is a CalculationRule listed on the field
        public List<FieldRule> Rules { get; set; } = [];
        public FormFieldExtensionValue? Extension { get; set; }
    }
}
