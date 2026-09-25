using SproutForms.Core.Models.Conditions;

namespace SproutForms.Umbraco.Core.Models.ViewModels
{
    public class FormPageViewModel
    {
        public int Index { get; init; }
        public string? Title { get; init; }
        public IReadOnlyList<FormRowViewModel> Rows { get; init; } = [];

        public required string NextLabel { get; init; }
        public required string PreviousLabel { get; init; }

        public ConditionDefinition? Visibility { get; init; }
    }
}
