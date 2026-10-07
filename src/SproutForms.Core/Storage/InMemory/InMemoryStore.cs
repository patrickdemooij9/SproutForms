using SproutForms.Core.Models;
using SproutForms.Core.Models.Flows;

namespace SproutForms.Core.Storage.InMemory
{
    /// <summary>
    /// What the in-memory repositories hold. One lock guards all of it, so a repository can read another's data, like a submission's form.
    /// </summary>
    public class InMemoryStore
    {
        internal readonly object Lock = new();

        internal Dictionary<Guid, Form> Forms { get; } = [];
        internal Dictionary<Guid, FormVersion> Versions { get; } = [];
        internal Dictionary<Guid, FormSubmission> Submissions { get; } = [];
        internal Dictionary<Guid, Folder> Folders { get; } = [];
        internal List<FormAuditEntry> AuditEntries { get; } = [];
        internal Dictionary<Guid, WorkflowExecution> Executions { get; } = [];
        internal Dictionary<Guid, WorkflowTemplate> Templates { get; } = [];

        internal Guid? GetFormId(FormSubmission submission)
            => Versions.TryGetValue(submission.FormVersionId, out var version) ? version.FormId : null;
    }
}
