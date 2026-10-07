using SproutForms.Core.Models;
using SproutForms.Core.Repositories;
using SproutForms.Umbraco.Core.Caching;
using SproutForms.Umbraco.Core.Extensions;
using SproutForms.Umbraco.Core.Models.Database;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Infrastructure.Scoping;
using Umbraco.Extensions;

namespace SproutForms.Umbraco.Core.Repositories
{
    public class FormRepository : IFormRepository
    {
        private readonly IScopeProvider _scopeProvider;
        private readonly Caching.IRepositoryCachePolicy<Form, Guid> _cachePolicy;
        private readonly DistributedCache _distributedCache;

        public FormRepository(IScopeProvider scopeProvider, IAppPolicyCache cache, DistributedCache distributedCache)
        {
            _scopeProvider = scopeProvider;
            _distributedCache = distributedCache;
            _cachePolicy = new Caching.DefaultRepositoryCachePolicy<Form, Guid>(cache, new RepositoryPolicyOptions<Form, Guid>(PerformCount, it => it.Id));
        }

        public void Delete(Guid formId)
        {
            _cachePolicy.Delete(formId, DoDelete);
            _distributedCache.RefreshSproutForms();
        }

        private void DoDelete(Guid formId)
        {
            using var scope = _scopeProvider.CreateScope(autoComplete: true);
            scope.Database.Delete<FormEntity>(formId);
        }

        public Form[] Get(int skip, int take, out int total)
        {
            var forms = _cachePolicy.GetAll(null, DoGetAll).Where(it => !it.IsTrashed).ToArray();
            total = forms.Length;
            return [.. forms.Skip(skip).Take(take)];
        }

        public Form? GetByAlias(string alias)
        {
            return _cachePolicy.GetAll(null, DoGetAll).FirstOrDefault(it => it.Alias.Equals(alias)); //TODO: this is not ideal, we should have a way to cache by alias as well, but for now this is better than hitting the db every time for an alias lookup
        }

        public Form[] GetByFolder(Guid? folderId, int skip, int take, out int total)
        {
            var entities = _cachePolicy.GetAll(null, DoGetAll).Where(it => it.FolderId == folderId && !it.IsTrashed).ToArray();
            total = entities.Length;
            return [.. entities.Skip(skip).Take(take)];
        }

        public Form[] GetTrashed(int skip, int take, out int total)
        {
            var forms = _cachePolicy.GetAll(null, DoGetAll).Where(it => it.IsTrashed).OrderByDescending(it => it.TrashedAt).ToArray();
            total = forms.Length;
            return [.. forms.Skip(skip).Take(take)];
        }

        public void MoveToRecycleBin(Guid formId, DateTime trashedAt, string trashedBy)
        {
            var form = GetById(formId) ?? throw new ArgumentException($"Could not find form with ID: {formId}");
            var trashed = Copy(form);
            trashed.TrashedAt = trashedAt;
            trashed.TrashedBy = trashedBy;
            _cachePolicy.Update(trashed, DoSave);
            _distributedCache.RefreshSproutForms();
        }

        public void RestoreFromRecycleBin(Guid formId, Guid? folderId)
        {
            var form = GetById(formId) ?? throw new ArgumentException($"Could not find form with ID: {formId}");
            var restored = Copy(form);
            restored.FolderId = folderId;
            restored.TrashedAt = null;
            restored.TrashedBy = null;
            _cachePolicy.Update(restored, DoSave);
            _distributedCache.RefreshSproutForms();
        }

        // The cache hands out its own instances, so changes are made to a copy
        private static Form Copy(Form form) => new()
        {
            Id = form.Id,
            Name = form.Name,
            Alias = form.Alias,
            Source = form.Source,
            FolderId = form.FolderId,
            TrashedAt = form.TrashedAt,
            TrashedBy = form.TrashedBy
        };

        public Form? GetById(Guid formId)
        {
            return _cachePolicy.Get(formId, DoGetById);
        }

        private Form? DoGetById(Guid id)
        {
            using var scope = _scopeProvider.CreateScope(autoComplete: true);
            var entity = scope.Database.FirstOrDefault<FormEntity>(scope.SqlContext.Sql()
                .SelectAll()
                .From<FormEntity>()
                .Where<FormEntity>(it => it.Id == id));

            return entity is null ? null : Map(entity);
        }

        private Form[] DoGetAll(Guid[]? ids)
        {
            using var scope = _scopeProvider.CreateScope(autoComplete: true);

            var sql = scope.SqlContext.Sql()
                .SelectAll()
                .From<FormEntity>();
            if (ids != null && ids.Length > 0)
            {
                sql = sql.WhereIn<FormEntity>(it => it.Id, ids);
            }
            var entities = scope.Database.Fetch<FormEntity>(sql);

            return [.. entities.Select(Map)];
        }

        private static Form Map(FormEntity entity) => new()
        {
            Id = entity.Id,
            Name = entity.Name,
            Alias = entity.Alias,
            Source = (FormSource)entity.Source,
            FolderId = entity.FolderId,
            // The database gives the value back without its kind
            TrashedAt = entity.TrashedAt is { } trashedAt ? DateTime.SpecifyKind(trashedAt, DateTimeKind.Utc) : null,
            TrashedBy = entity.TrashedBy
        };

        public Guid Save(Form form)
        {
            if (form.Id == Guid.Empty)
            {
                form.Id = Guid.NewGuid();
            }

            _cachePolicy.Create(form, DoSave);
            _distributedCache.RefreshSproutForms();
            return form.Id;
        }

        private void DoSave(Form form)
        {
            using var scope = _scopeProvider.CreateScope(autoComplete: true);
            scope.Database.Save(new FormEntity
            {
                Id = form.Id,
                Name = form.Name,
                Alias = form.Alias,
                Source = (int)form.Source,
                FolderId = form.FolderId,
                TrashedAt = form.TrashedAt,
                TrashedBy = form.TrashedBy
            });
        }

        private int PerformCount()
        {
            using var scope = _scopeProvider.CreateScope(autoComplete: true);
            return scope.Database.ExecuteScalar<int>(scope.SqlContext.Sql()
                .SelectCount()
                .From<FormEntity>());
        }
    }
}
