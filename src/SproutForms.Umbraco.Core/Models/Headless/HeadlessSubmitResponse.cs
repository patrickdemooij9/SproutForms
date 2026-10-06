using System.Text.Json;

namespace SproutForms.Umbraco.Core.Models.Headless
{
    public sealed class HeadlessSubmitResponse
    {
        // Null when the form's outcome couldn't run; the submission is saved all the same, so show a confirmation anyway
        public HeadlessOutcome? Outcome { get; init; }

        // The values of the variables the browser gets the calculations of, by alias, because a field's or page's condition uses them
        public IReadOnlyDictionary<string, JsonElement> Variables { get; init; } = new Dictionary<string, JsonElement>();
    }

    public sealed class HeadlessOutcome
    {
        public required string Type { get; init; }

        // Depends on the outcome type: "message" has message, "redirect" has url, "redirectUmbracoPage" has url, path and contentKey
        public IReadOnlyDictionary<string, object?> Data { get; init; } = new Dictionary<string, object?>();
    }
}
