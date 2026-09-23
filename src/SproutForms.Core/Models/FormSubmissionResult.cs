using System.Text.Json;

namespace SproutForms.Core.Models
{
    public class FormSubmissionResult
    {
        public bool IsValid => Errors.Count == 0;

        public IReadOnlyDictionary<string, List<string>> Errors { get; init; }
        = new Dictionary<string, List<string>>();

        public IReadOnlyDictionary<string, JsonElement> Values { get; init; }
            = new Dictionary<string, JsonElement>();

        // The saved submission; null when it was rejected
        public FormSubmission? Submission { get; init; }
    }
}
