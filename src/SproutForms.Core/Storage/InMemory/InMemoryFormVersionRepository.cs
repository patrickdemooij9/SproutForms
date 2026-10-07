using SproutForms.Core.Models;
using SproutForms.Core.Repositories;

namespace SproutForms.Core.Storage.InMemory
{
    public class InMemoryFormVersionRepository : IFormVersionRepository
    {
        private readonly InMemoryStore _store;

        public InMemoryFormVersionRepository(InMemoryStore store)
        {
            _store = store;
        }

        public FormVersion? GetPublished(Guid formId)
        {
            lock (_store.Lock)
            {
                return _store.Versions.Values.FirstOrDefault(it => it.FormId == formId && it.Status == FormStatus.Published);
            }
        }

        public FormVersion? GetLatest(Guid formId)
        {
            lock (_store.Lock)
            {
                return _store.Versions.Values.Where(it => it.FormId == formId).MaxBy(it => it.Version);
            }
        }

        public FormVersion? Get(Guid formVersionId)
        {
            lock (_store.Lock)
            {
                return _store.Versions.GetValueOrDefault(formVersionId);
            }
        }

        public IReadOnlyList<FormVersion> GetAll(Guid formId)
        {
            lock (_store.Lock)
            {
                return _store.Versions.Values.Where(it => it.FormId == formId).OrderByDescending(it => it.Version).ToList();
            }
        }

        public void Add(FormVersion version)
        {
            lock (_store.Lock)
            {
                // A form has one published version
                if (version.Status == FormStatus.Published)
                {
                    UnpublishAll(version.FormId);
                }
                _store.Versions[version.Id] = version;
            }
        }

        public void Publish(Guid versionId)
        {
            lock (_store.Lock)
            {
                if (!_store.Versions.TryGetValue(versionId, out var version)) return;
                UnpublishAll(version.FormId);
                version.Status = FormStatus.Published;
            }
        }

        public void DeleteAllByForm(Guid formId)
        {
            lock (_store.Lock)
            {
                foreach (var id in _store.Versions.Values.Where(it => it.FormId == formId).Select(it => it.Id).ToList())
                {
                    _store.Versions.Remove(id);
                }
            }
        }

        private void UnpublishAll(Guid formId)
        {
            foreach (var published in _store.Versions.Values.Where(it => it.FormId == formId && it.Status == FormStatus.Published))
            {
                published.Status = FormStatus.Draft;
            }
        }
    }
}
