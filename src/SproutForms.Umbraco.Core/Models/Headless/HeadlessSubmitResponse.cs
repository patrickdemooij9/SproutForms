namespace SproutForms.Umbraco.Core.Models.Headless
{
    public sealed class HeadlessSubmitResponse
    {
        // Null when the form's outcome couldn't run; the submission is saved all the same, so show a confirmation anyway
        public HeadlessOutcome? Outcome { get; init; }
    }

    public sealed class HeadlessOutcome
    {
        public required string Type { get; init; }

        // Depends on the outcome type: "message" has message, "redirect" has url, "redirectUmbracoPage" has url, path and contentKey
        public IReadOnlyDictionary<string, object?> Data { get; init; } = new Dictionary<string, object?>();
    }
}
