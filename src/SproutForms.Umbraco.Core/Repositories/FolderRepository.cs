using SproutForms.Core.Models;
using SproutForms.Core.Repositories;
using SproutForms.Umbraco.Core.Models.Database;
using Umbraco.Cms.Infrastructure.Scoping;
using Umbraco.Extensions;

namespace SproutForms.Umbraco.Core.Repositories
{
    public class FolderRepository : IFolderRepository
    {
        private readonly IScopeProvider _scopeProvider;

        public FolderRepository(IScopeProvider scopeProvider)
        {
            _scopeProvider = scopeProvider;
        }

        public void Delete(Guid folderId)
        {
            using var scope = _scopeProvider.CreateScope(autoComplete: true);
            scope.Database.Delete<FolderEntity>(folderId);
        }

        public Folder[] GetChildFolders(Guid parentId)
        {
            using var scope = _scopeProvider.CreateScope(autoComplete: true);
            var entities = scope.Database.Fetch<FolderEntity>(scope.SqlContext.Sql()
                .SelectAll()
                .From<FolderEntity>()
                .Where<FolderEntity>(it => it.ParentId == parentId)
                .OrderBy<FolderEntity>(it => it.SortOrder));

            return entities.Select(it => new Folder
            {
                Id = it.Id,
                Name = it.Name,
                ParentId = it.ParentId,
                SortOrder = it.SortOrder
            }).ToArray();
        }

        public Folder? GetById(Guid folderId)
        {
            using var scope = _scopeProvider.CreateScope(autoComplete: true);
            var entity = scope.Database.FirstOrDefault<FolderEntity>(scope.SqlContext.Sql()
                .SelectAll()
                .From<FolderEntity>()
                .Where<FolderEntity>(it => it.Id == folderId));

            return entity is null ? null : new Folder
            {
                Id = entity.Id,
                Name = entity.Name,
                ParentId = entity.ParentId,
                SortOrder = entity.SortOrder
            };
        }

        public Folder[] GetRootFolders()
        {
            using var scope = _scopeProvider.CreateScope(autoComplete: true);
            var entities = scope.Database.Fetch<FolderEntity>(scope.SqlContext.Sql()
                .SelectAll()
                .From<FolderEntity>()
                .Where<FolderEntity>(it => it.ParentId == null)
                .OrderBy<FolderEntity>(it => it.SortOrder));

            return entities.Select(it => new Folder
            {
                Id = it.Id,
                Name = it.Name,
                ParentId = it.ParentId,
                SortOrder = it.SortOrder
            }).ToArray();
        }

        public Guid Save(Folder folder)
        {
            using var scope = _scopeProvider.CreateScope(autoComplete: true);
            if (folder.Id == Guid.Empty)
            {
                folder.Id = Guid.NewGuid();
            }

            scope.Database.Save(new FolderEntity
            {
                Id = folder.Id,
                Name = folder.Name,
                ParentId = folder.ParentId,
                SortOrder = folder.SortOrder
            });
            return folder.Id;
        }
    }
}
