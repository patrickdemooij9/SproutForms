namespace SproutForms.Core.Models.FormTypes
{
    /// <summary>
    /// What a form type makes of a submission: results to store with it, such as a quiz score, or errors that reject it.
    /// </summary>
    public sealed class FormTypeSubmissionResult
    {
        public IReadOnlyDictionary<string, object?> Results { get; init; } = new Dictionary<string, object?>();

        // Keyed by field alias, like field validation errors; a key that isn't a field shows at the top of the form
        public IReadOnlyDictionary<string, List<string>> Errors { get; init; } = new Dictionary<string, List<string>>();

        public static FormTypeSubmissionResult None { get; } = new();

        public static FormTypeSubmissionResult WithResults(IReadOnlyDictionary<string, object?> results)
            => new() { Results = results };

        public static FormTypeSubmissionResult Reject(string key, string message)
            => new() { Errors = new Dictionary<string, List<string>> { [key] = [message] } };
    }
}
