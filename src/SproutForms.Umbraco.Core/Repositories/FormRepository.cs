using SproutForms.Core.Models;
using SproutForms.Core.Repositories;
using SproutForms.Umbraco.Core.Caching;
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

        public FormRepository(IScopeProvider scopeProvider, IAppPolicyCache cache)
        {
            _scopeProvider = scopeProvider;
            _cachePolicy = new Caching.DefaultRepositoryCachePolicy<Form, Guid>(cache, new RepositoryPolicyOptions<Form, Guid>(PerformCount, it => it.Id));
        }

        public void Delete(Guid formId)
        {
            _cachePolicy.Delete(formId, DoDelete);
        }

        private void DoDelete(Guid formId)
        {
            using var scope = _scopeProvider.CreateScope(autoComplete: true);
            scope.Database.Delete<FormEntity>(formId);
        }

        public Form[] Get(int skip, int take, out int total)
        {
            var forms = _cachePolicy.GetAll(null, DoGetAll);
            total = forms.Length;
            return [.. forms.Skip(skip).Take(take)];
        }

        public Form? GetByAlias(string alias)
        {
            return _cachePolicy.GetAll(null, DoGetAll).FirstOrDefault(it => it.Alias.Equals(alias)); //TODO: this is not ideal, we should have a way to cache by alias as well, but for now this is better than hitting the db every time for an alias lookup
        }

        public Form[] GetByFolder(Guid? folderId, int skip, int take, out int total)
        {
            var entities = _cachePolicy.GetAll(null, DoGetAll).Where(it => it.FolderId == folderId).ToArray();
            total = entities.Length;
            return [.. entities.Skip(skip).Take(take)];
        }

        public Form? GetById(Guid formId)
        {
            return _cachePolicy.Get(formId, DoGetById);
        }

        private Form? DoGetById(Guid id)
        {
            Console.WriteLine("Cache fail!");

            using var scope = _scopeProvider.CreateScope(autoComplete: true);
            var entity = scope.Database.FirstOrDefault<FormEntity>(scope.SqlContext.Sql()
                .SelectAll()
                .From<FormEntity>()
                .Where<FormEntity>(it => it.Id == id));

            return entity is null ? null : new Form
            {
                Id = entity.Id,
                Name = entity.Name,
                Alias = entity.Alias,
                Source = (FormSource)entity.Source,
                FolderId = entity.FolderId
            };
        }

        private Form[] DoGetAll(Guid[]? ids)
        {
            Console.WriteLine("Cache fail!");
            using var scope = _scopeProvider.CreateScope(autoComplete: true);

            var sql = scope.SqlContext.Sql()
                .SelectAll()
                .From<FormEntity>();
            if (ids != null && ids.Length > 0)
            {
                sql = sql.WhereIn<FormEntity>(it => it.Id, ids);
            }
            var entities = scope.Database.Fetch<FormEntity>(sql);

            return [.. entities.Select(it => new Form
            {
                Id = it.Id,
                Name = it.Name,
                Alias = it.Alias,
                Source = (FormSource)it.Source,
                FolderId = it.FolderId
            })];
        }

        public Guid Save(Form form)
        {
            if (form.Id == Guid.Empty)
            {
                form.Id = Guid.NewGuid();
            }

            _cachePolicy.Create(form, DoCreate);
            return form.Id;
        }

        private void DoCreate(Form form)
        {
            using var scope = _scopeProvider.CreateScope(autoComplete: true);
            scope.Database.Save(new FormEntity
            {
                Id = form.Id,
                Name = form.Name,
                Alias = form.Alias,
                Source = (int)form.Source,
                FolderId = form.FolderId
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
