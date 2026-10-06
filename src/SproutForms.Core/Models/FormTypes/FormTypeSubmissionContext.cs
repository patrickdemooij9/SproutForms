using System.Text.Json;

namespace SproutForms.Core.Models.FormTypes
{
    /// <summary>
    /// A submission that passed field validation and is about to be saved, as its form type sees it.
    /// </summary>
    public sealed class FormTypeSubmissionContext
    {
        public required FormVersion Version { get; init; }
        public required IReadOnlyDictionary<string, JsonElement> Values { get; init; }

        // What the form's calculations worked out, by variable alias
        public IReadOnlyDictionary<string, JsonElement> Variables { get; init; } = new Dictionary<string, JsonElement>();

        public FormDefinition Definition => Version.Definition;
    }
}
