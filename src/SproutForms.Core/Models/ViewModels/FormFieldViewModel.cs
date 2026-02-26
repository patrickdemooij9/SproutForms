using SproutForms.Core.Models;

namespace SproutForms.Umbraco.Core.Models.ViewModels
{
    public class FormFieldViewModel
    {
        public string Alias { get; init; } = default!;
        public string Label { get; init; } = default!;
        public string Type { get; init; } = default!;
        public bool Required { get; init; }
        public bool RendersOwnLabel { get; init; }

        public object Configuration { get; init; } = default!;

        public object? Conditions { get; init; }

        public string[] Errors { get; set; } = [];
        public string? Value { get; set; }
        
        public IEnumerable<ValidationRule> ValidationRules { get; set; } = [];
    }
}
