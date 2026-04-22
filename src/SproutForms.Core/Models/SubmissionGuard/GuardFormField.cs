namespace SproutForms.Core.Models.SubmissionGuard
{
    public class GuardFormField
    {
        public required string Name { get; init; }
        public string Type { get; init; } = "text";
        public string Value { get; init; } = "";
        public string? Id { get; init; }
        public string? Label { get; init; }
        public bool VisuallyHidden { get; init; }
        public string? AutoComplete { get; init; }
        public int? TabIndex { get; init; }
    }
}
