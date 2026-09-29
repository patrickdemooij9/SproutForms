namespace SproutForms.Core.Models.ClientModels
{
    /// <summary>
    /// A published form as a front-end sees it: its pages, rows and columns, each column holding its field with what it needs to render, validate and submit.
    /// It holds nothing a visitor mustn't see, so the headless API returns it as-is, and the Razor view model is built from it.
    /// </summary>
    public sealed class FormClientModel
    {
        public required Guid Id { get; init; }
        public required string Alias { get; init; }

        // Changes with every publish, so a front-end can tell whether its copy is out of date
        public required Guid VersionId { get; init; }

        public required string FormType { get; init; }
        public object? FormTypeSettings { get; init; }

        public required string SubmitLabel { get; init; }
        public bool ShowProgress { get; init; }

        // Every field is in exactly one column of one page, the same shape as the Razor view model
        public IReadOnlyList<FormClientPage> Pages { get; init; } = [];

        public FormClientSubmissionGuard? SubmissionGuard { get; init; }

        public required FormClientTexts Texts { get; init; }
    }
}
