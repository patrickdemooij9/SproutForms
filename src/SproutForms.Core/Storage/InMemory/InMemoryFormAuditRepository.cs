using SproutForms.Core.Models;
using SproutForms.Core.Repositories;

namespace SproutForms.Core.Storage.InMemory
{
    public class InMemoryFormAuditRepository : IFormAuditRepository
    {
        private readonly InMemoryStore _store;

        public InMemoryFormAuditRepository(InMemoryStore store)
        {
            _store = store;
        }

        public void Add(FormAuditEntry entry)
        {
            lock (_store.Lock)
            {
                if (entry.Id == Guid.Empty)
                {
                    entry.Id = Guid.NewGuid();
                }
                _store.AuditEntries.Add(entry);
            }
        }

        public IReadOnlyList<FormAuditEntry> GetByForm(Guid formId, int skip, int take, out int totalCount)
        {
            lock (_store.Lock)
            {
                var entries = _store.AuditEntries.Where(it => it.FormId == formId).OrderByDescending(it => it.CreatedAt).ToList();
                totalCount = entries.Count;
                return entries.Skip(skip).Take(take).ToList();
            }
        }

        public void DeleteAllByForm(Guid formId)
        {
            lock (_store.Lock)
            {
                _store.AuditEntries.RemoveAll(it => it.FormId == formId);
            }
        }
    }
}
