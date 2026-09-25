using SproutForms.Core.Models;

namespace SproutForms.Core.Repositories
{
    public interface IFormAuditRepository
    {
        void Add(FormAuditEntry entry);

        // Newest first
        IReadOnlyList<FormAuditEntry> GetByForm(Guid formId, int skip, int take, out int totalCount);
        void DeleteAllByForm(Guid formId);
    }
}
