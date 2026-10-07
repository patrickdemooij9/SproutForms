using SproutForms.Core.Models;
using SproutForms.Core.Repositories;

namespace SproutForms.Core.Storage.InMemory
{
    public class InMemoryFormRepository : IFormRepository
    {
        private readonly InMemoryStore _store;

        public InMemoryFormRepository(InMemoryStore store)
        {
            _store = store;
        }

        public Form[] Get(int skip, int take, out int total)
            => Page(it => !it.IsTrashed, it => it.Name, skip, take, out total);

        public Form[] GetByFolder(Guid? folderId, int skip, int take, out int total)
            => Page(it => !it.IsTrashed && it.FolderId == folderId, it => it.Name, skip, take, out total);

        public Form[] GetTrashed(int skip, int take, out int total)
        {
            lock (_store.Lock)
            {
                var forms = _store.Forms.Values.Where(it => it.IsTrashed).OrderByDescending(it => it.TrashedAt).ToArray();
                total = forms.Length;
                return forms.Skip(skip).Take(take).ToArray();
            }
        }

        public Form? GetByAlias(string alias)
        {
            lock (_store.Lock)
            {
                return _store.Forms.Values.FirstOrDefault(it => string.Equals(it.Alias, alias, StringComparison.OrdinalIgnoreCase));
            }
        }

        public Form? GetById(Guid formId)
        {
            lock (_store.Lock)
            {
                return _store.Forms.GetValueOrDefault(formId);
            }
        }

        public Guid Save(Form form)
        {
            lock (_store.Lock)
            {
                if (form.Id == Guid.Empty)
                {
                    form.Id = Guid.NewGuid();
                }
                _store.Forms[form.Id] = form;
                return form.Id;
            }
        }

        public void MoveToRecycleBin(Guid formId, DateTime trashedAt, string trashedBy)
        {
            lock (_store.Lock)
            {
                if (!_store.Forms.TryGetValue(formId, out var form)) return;
                form.TrashedAt = trashedAt;
                form.TrashedBy = trashedBy;
            }
        }

        public void RestoreFromRecycleBin(Guid formId, Guid? folderId)
        {
            lock (_store.Lock)
            {
                if (!_store.Forms.TryGetValue(formId, out var form)) return;
                form.TrashedAt = null;
                form.TrashedBy = null;
                form.FolderId = folderId;
            }
        }

        public void Delete(Guid formId)
        {
            lock (_store.Lock)
            {
                _store.Forms.Remove(formId);
            }
        }

        private Form[] Page(Func<Form, bool> filter, Func<Form, string> orderBy, int skip, int take, out int total)
        {
            lock (_store.Lock)
            {
                var forms = _store.Forms.Values.Where(filter).OrderBy(orderBy).ToArray();
                total = forms.Length;
                return forms.Skip(skip).Take(take).ToArray();
            }
        }
    }
}
