using SproutForms.Core.Models;

namespace SproutForms.Core.Repositories
{
    public interface IFolderRepository
    {
        Folder[] GetRootFolders();
        Folder[] GetChildFolders(Guid parentId);
        Folder? GetById(Guid folderId);
        Guid Save(Folder folder);
        void Delete(Guid folderId);
    }
}
