using SproutForms.Core.Models;
using SproutForms.Core.Repositories;

namespace SproutForms.Core.Storage.InMemory
{
    public class InMemoryFolderRepository : IFolderRepository
    {
        private readonly InMemoryStore _store;

        public InMemoryFolderRepository(InMemoryStore store)
        {
            _store = store;
        }

        public Folder[] GetRootFolders()
        {
            lock (_store.Lock)
            {
                return _store.Folders.Values.Where(it => it.ParentId is null).OrderBy(it => it.SortOrder).ToArray();
            }
        }

        public Folder[] GetChildFolders(Guid parentId)
        {
            lock (_store.Lock)
            {
                return _store.Folders.Values.Where(it => it.ParentId == parentId).OrderBy(it => it.SortOrder).ToArray();
            }
        }

        public Folder? GetById(Guid folderId)
        {
            lock (_store.Lock)
            {
                return _store.Folders.GetValueOrDefault(folderId);
            }
        }

        public Guid Save(Folder folder)
        {
            lock (_store.Lock)
            {
                if (folder.Id == Guid.Empty)
                {
                    folder.Id = Guid.NewGuid();
                }
                _store.Folders[folder.Id] = folder;
                return folder.Id;
            }
        }

        public void Delete(Guid folderId)
        {
            lock (_store.Lock)
            {
                _store.Folders.Remove(folderId);
            }
        }
    }
}
