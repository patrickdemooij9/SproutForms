using SproutForms.Core.Models;
using SproutForms.Core.Repositories;
using SproutForms.Umbraco.Core.Models.Database;
using Umbraco.Cms.Infrastructure.Scoping;
using Umbraco.Extensions;

namespace SproutForms.Umbraco.Core.Repositories
{
    public class FormAuditRepository : IFormAuditRepository
    {
        private readonly IScopeProvider _scopeProvider;

        public FormAuditRepository(IScopeProvider scopeProvider)
        {
            _scopeProvider = scopeProvider;
        }

        public void Add(FormAuditEntry entry)
        {
            if (entry.Id == Guid.Empty)
            {
                entry.Id = Guid.NewGuid();
            }

            using var scope = _scopeProvider.CreateScope(autoComplete: true);
            scope.Database.Insert(new FormAuditEntryEntity
            {
                Id = entry.Id,
                FormId = entry.FormId,
                Action = (int)entry.Action,
                UserKey = entry.UserKey,
                CreatedAt = entry.CreatedAt,
                VersionId = entry.VersionId,
                Comment = entry.Comment
            });
        }

        public IReadOnlyList<FormAuditEntry> GetByForm(Guid formId, int skip, int take, out int totalCount)
        {
            using var scope = _scopeProvider.CreateScope(autoComplete: true);
            totalCount = scope.Database.ExecuteScalar<int>(scope.SqlContext.Sql()
                .SelectCount()
                .From<FormAuditEntryEntity>()
                .Where<FormAuditEntryEntity>(it => it.FormId == formId));

            var entities = scope.Database.SkipTake<FormAuditEntryEntity>(skip, take, scope.SqlContext.Sql()
                .SelectAll()
                .From<FormAuditEntryEntity>()
                .Where<FormAuditEntryEntity>(it => it.FormId == formId)
                .OrderByDescending<FormAuditEntryEntity>(it => it.CreatedAt));

            return [.. entities.Select(entity => new FormAuditEntry
            {
                Id = entity.Id,
                FormId = entity.FormId,
                Action = (FormAuditAction)entity.Action,
                UserKey = entity.UserKey,
                CreatedAt = entity.CreatedAt,
                VersionId = entity.VersionId,
                Comment = entity.Comment
            })];
        }

        public void DeleteAllByForm(Guid formId)
        {
            using var scope = _scopeProvider.CreateScope(autoComplete: true);
            scope.Database.Execute("DELETE FROM SproutForms_FormAudit WHERE FormId = @0", formId);
        }
    }
}
