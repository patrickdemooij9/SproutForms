using SproutForms.Core.Models;
using System.Text.Json.Serialization;

namespace SproutForms.Umbraco.Core.Models.ViewModels
{
    public class FormAuditEntryBackofficeModel
    {
        public Guid Id { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public FormAuditAction Action { get; set; }
        public required string UserName { get; set; }
        public DateTime CreatedAt { get; set; }

        // The number of the version the action created, when it created one
        public int? Version { get; set; }
        public string? Comment { get; set; }
    }
}
