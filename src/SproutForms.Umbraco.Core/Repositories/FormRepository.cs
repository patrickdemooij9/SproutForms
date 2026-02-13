using SproutForms.Core.Models;
using SproutForms.Core.Repositories;
using SproutForms.Umbraco.Core.Models.Database;
using System;
using System.Collections.Generic;
using System.Text;
using Umbraco.Cms.Infrastructure.Scoping;
using Umbraco.Extensions;

namespace SproutForms.Umbraco.Core.Repositories
{
    public class FormRepository : IFormRepository
    {
        private readonly IScopeProvider _scopeProvider;

        public FormRepository(IScopeProvider scopeProvider)
        {
            _scopeProvider = scopeProvider;
        }

        public void Delete(Guid formId)
        {
            using var scope = _scopeProvider.CreateScope(autoComplete: true);
            scope.Database.Delete<FormEntity>(formId);
        }

        public Form[] Get(int skip, int take, out int total)
        {
            using var scope = _scopeProvider.CreateScope(autoComplete: true);
            var entities = scope.Database.Fetch<FormEntity>(scope.SqlContext.Sql()
                .SelectAll()
                .From<FormEntity>());
            total = entities.Count;

            return entities.Skip(skip).Take(take).Select(it => new Form
            {
                Id = it.Id,
                Name = it.Name,
                Alias = it.Alias,
                Source = (FormSource)it.Source,
                FolderId = it.FolderId
            }).ToArray();
        }

        public Form[] GetByFolder(Guid? folderId, int skip, int take, out int total)
        {
            using var scope = _scopeProvider.CreateScope(autoComplete: true);
            var sql = scope.SqlContext.Sql()
                .SelectAll()
                .From<FormEntity>();

            if (folderId.HasValue)
            {
                sql = sql.Where<FormEntity>(it => it.FolderId == folderId.Value);
            }
            else
            {
                sql = sql.Where<FormEntity>(it => it.FolderId == null);
            }

            var entities = scope.Database.Fetch<FormEntity>(sql);
            total = entities.Count;

            return entities.Skip(skip).Take(take).Select(it => new Form
            {
                Id = it.Id,
                Name = it.Name,
                Alias = it.Alias,
                Source = (FormSource)it.Source,
                FolderId = it.FolderId
            }).ToArray();
        }

        public Form? GetByAlias(string alias)
        {
            using var scope = _scopeProvider.CreateScope(autoComplete: true);
            var entity = scope.Database.FirstOrDefault<FormEntity>(scope.SqlContext.Sql()
                .SelectAll()
                .From<FormEntity>()
                .Where<FormEntity>(it => it.Alias == alias));

            return entity is null ? null : new Form
            {
                Id = entity.Id,
                Name = entity.Name,
                Alias = entity.Alias,
                Source = (FormSource)entity.Source,
                FolderId = entity.FolderId
            };
        }

        public Form? GetById(Guid formId)
        {
            using var scope = _scopeProvider.CreateScope(autoComplete: true);
            var entity = scope.Database.FirstOrDefault<FormEntity>(scope.SqlContext.Sql()
                .SelectAll()
                .From<FormEntity>()
                .Where<FormEntity>(it => it.Id == formId));

            return entity is null ? null : new Form
            {
                Id = entity.Id,
                Name = entity.Name,
                Alias = entity.Alias,
                Source = (FormSource)entity.Source,
                FolderId = entity.FolderId
            };
        }

        public Guid Save(Form form)
        {
            using var scope = _scopeProvider.CreateScope(autoComplete: true);
            if (form.Id == Guid.Empty)
            {
                form.Id = Guid.NewGuid();
            }

            scope.Database.Save(new FormEntity
            {
                Id = form.Id,
                Name = form.Name,
                Alias = form.Alias,
                Source = (int)form.Source,
                FolderId = form.FolderId
            });
            return form.Id;
        }
    }
}
