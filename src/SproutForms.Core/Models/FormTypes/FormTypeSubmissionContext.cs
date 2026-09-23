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

        public FormDefinition Definition => Version.Definition;
    }
}
