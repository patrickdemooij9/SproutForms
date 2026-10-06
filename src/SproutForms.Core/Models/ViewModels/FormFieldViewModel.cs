using SproutForms.Core.Models;
using SproutForms.Core.Models.Conditions;

namespace SproutForms.Umbraco.Core.Models.ViewModels
{
    public class FormFieldViewModel
    {
        // The field's path: its alias, or inside a field group's entry the alias after the entry, such as "people[0].firstName"
        public string Alias { get; init; } = default!;
        public string Label { get; init; } = default!;
        public string Type { get; init; } = default!;
        public bool Required { get; init; }
        public bool RendersOwnLabel { get; init; }

        public object Configuration { get; init; } = default!;

        public IReadOnlyList<FieldRule> Rules { get; init; } = [];

        public string[] Errors { get; set; } = [];
        public string? Value { get; set; }
        
        public IEnumerable<ValidationRule> ValidationRules { get; set; } = [];

        // For a field group such as a repeater: the entries to render, and the one forms.js copies when the visitor adds an entry
        public IReadOnlyList<FormFieldGroupEntryViewModel> Entries { get; init; } = [];
        public FormFieldGroupEntryViewModel? EntryTemplate { get; init; }
    }
}
