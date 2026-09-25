namespace SproutForms.Core.Models
{
    /// <summary>
    /// Something that happened to a form, shown in the form's history.
    /// </summary>
    public class FormAuditEntry
    {
        public Guid Id { get; set; }
        public Guid FormId { get; set; }
        public FormAuditAction Action { get; set; }

        // The backoffice user's key, or "System" for changes made by code-first forms
        public required string UserKey { get; set; }
        public DateTime CreatedAt { get; set; }

        // The version the action created, when it created one
        public Guid? VersionId { get; set; }
        public string? Comment { get; set; }
    }
}
