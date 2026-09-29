using SproutForms.Core.Models.Conditions;

namespace SproutForms.Core.Models.ClientModels
{
    public sealed class FormClientPage
    {
        public int Index { get; init; }
        public string? Title { get; init; }

        // The title, or "Step n" for a page without one
        public required string ProgressLabel { get; init; }

        public required string NextLabel { get; init; }
        public required string PreviousLabel { get; init; }

        public ConditionDefinition? Visibility { get; init; }

        public IReadOnlyList<FormClientRow> Rows { get; init; } = [];
    }
}
