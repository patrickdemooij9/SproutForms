using System.Text.Json.Serialization;

namespace SproutForms.Umbraco.Core.Models.ViewModels
{
    /// <summary>
    /// What rolling back to a version would change in the current version of the form.
    /// </summary>
    public class FormVersionComparisonBackofficeModel
    {
        public required FormVersionBackofficeModel Version { get; set; }
        public List<FormChangeBackofficeModel> Changes { get; set; } = [];

        // Labels of the fields the rollback removes that existing submissions have values for
        public List<string> RemovedFieldsWithSubmissions { get; set; } = [];

        // Why the version can't be rolled back to; empty when it can
        public List<string> RollbackErrors { get; set; } = [];
    }

    public class FormChangeBackofficeModel
    {
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public FormChangeSection Section { get; set; }
        public required string Name { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public FormChangeType ChangeType { get; set; }
        public List<FormPropertyChangeBackofficeModel> Properties { get; set; } = [];
    }

    public class FormPropertyChangeBackofficeModel
    {
        public required string Name { get; set; }
        public string? CurrentValue { get; set; }
        public string? VersionValue { get; set; }
    }

    public enum FormChangeSection
    {
        Form,
        Page,
        Field,
        Workflow,
        Outcome
    }

    // Seen from the current version: Added means the rollback brings the item back
    public enum FormChangeType
    {
        Added,
        Removed,
        Changed
    }
}
