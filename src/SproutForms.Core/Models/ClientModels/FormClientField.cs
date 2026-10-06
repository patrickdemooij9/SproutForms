using SproutForms.Core.Models.Conditions;

namespace SproutForms.Core.Models.ClientModels
{
    public sealed class FormClientField
    {
        public required string Alias { get; init; }
        public required string Label { get; init; }
        public required string Type { get; init; }
        public bool Required { get; init; }
        public bool RendersOwnLabel { get; init; }

        // From IFormFieldType.GetClientConfiguration, so without settings only the server needs
        public object? Configuration { get; init; }

        public IReadOnlyList<FieldRule> Rules { get; init; } = [];
        public IReadOnlyList<ValidationRule> ValidationRules { get; init; } = [];

        // From IFormDefinitionType.GetClientFieldExtension; nothing unless the form type shares it
        public object? Extension { get; init; }

        // The layout of each entry of a field group, such as a repeater; null for any other field
        public IReadOnlyList<FormClientRow>? Rows { get; init; }
    }
}
