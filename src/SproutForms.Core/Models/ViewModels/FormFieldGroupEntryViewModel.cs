namespace SproutForms.Umbraco.Core.Models.ViewModels
{
    /// <summary>
    /// One entry of a field group such as a repeater. Its fields' aliases start with the entry's prefix, such as "people[0].".
    /// </summary>
    public sealed class FormFieldGroupEntryViewModel
    {
        // The placeholder the entry template has instead of an index; forms.js replaces it when the visitor adds an entry
        public const string IndexPlaceholder = "__index__";

        public required string Prefix { get; init; }

        // Null for the entry template, whose title forms.js fills in
        public string? Title { get; init; }
        public required string RemoveLabel { get; init; }
        public IReadOnlyList<FormRowViewModel> Rows { get; init; } = [];
    }
}
