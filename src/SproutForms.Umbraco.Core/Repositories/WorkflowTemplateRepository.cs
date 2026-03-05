using System.Text.Json;
using SproutForms.Core.Models;
using SproutForms.Core.Repositories;
using SproutForms.Umbraco.Core.Models.Database;
using Umbraco.Cms.Infrastructure.Scoping;
using Umbraco.Extensions;

namespace SproutForms.Umbraco.Core.Repositories
{
    public class WorkflowTemplateRepository : IWorkflowTemplateRepository
    {
        private readonly IScopeProvider _scopeProvider;

        public WorkflowTemplateRepository(IScopeProvider scopeProvider)
        {
            _scopeProvider = scopeProvider;
        }

        public WorkflowTemplate? GetById(Guid id)
        {
            using var scope = _scopeProvider.CreateScope(autoComplete: true);
            var entity = scope.Database.FirstOrDefault<WorkflowTemplateEntity>(
                scope.SqlContext.Sql()
                    .SelectAll()
                    .From<WorkflowTemplateEntity>()
                    .Where<WorkflowTemplateEntity>(it => it.Id == id));

            return entity is null ? null : MapToModel(entity);
        }

        public IReadOnlyList<WorkflowTemplate> GetAll()
        {
            using var scope = _scopeProvider.CreateScope(autoComplete: true);
            var entities = scope.Database.Fetch<WorkflowTemplateEntity>(
                scope.SqlContext.Sql()
                    .SelectAll()
                    .From<WorkflowTemplateEntity>()
                    .OrderBy<WorkflowTemplateEntity>(it => it.Name));

            return entities.Select(MapToModel).ToList();
        }

        public IReadOnlyList<WorkflowTemplate> GetByWorkflowType(string workflowTypeAlias)
        {
            using var scope = _scopeProvider.CreateScope(autoComplete: true);
            var entities = scope.Database.Fetch<WorkflowTemplateEntity>(
                scope.SqlContext.Sql()
                    .SelectAll()
                    .From<WorkflowTemplateEntity>()
                    .Where<WorkflowTemplateEntity>(it => it.WorkflowTypeAlias == workflowTypeAlias)
                    .OrderBy<WorkflowTemplateEntity>(it => it.Name));

            return entities.Select(MapToModel).ToList();
        }

        public Guid Save(WorkflowTemplate template)
        {
            using var scope = _scopeProvider.CreateScope(autoComplete: true);

            WorkflowTemplateEntity entity;
            if (template.Id == Guid.Empty)
            {
                entity = new WorkflowTemplateEntity
                {
                    Id = Guid.NewGuid(),
                    CreatedAt = DateTime.UtcNow
                };
            }
            else
            {
                entity = scope.Database.FirstOrDefault<WorkflowTemplateEntity>(
                scope.SqlContext.Sql()
                    .SelectAll()
                    .From<WorkflowTemplateEntity>()
                    .Where<WorkflowTemplateEntity>(it => it.Id == template.Id)) ?? throw new ArgumentException($"Could not find template with ID: {template.Id}");
            }

            entity.Name = template.Name;
            entity.WorkflowTypeAlias = template.WorkflowTypeAlias;
            entity.ConfigurationJson = template.Configuration.GetRawText();
            entity.LockedFieldsJson = JsonSerializer.Serialize(template.LockedFields);
            entity.UpdatedAt = DateTime.UtcNow;

            scope.Database.Save(entity);
            return template.Id;
        }

        public void Delete(Guid id)
        {
            using var scope = _scopeProvider.CreateScope(autoComplete: true);
            scope.Database.Delete<WorkflowTemplateEntity>(id);
        }

        private static WorkflowTemplate MapToModel(WorkflowTemplateEntity entity)
        {
            var lockedFields = JsonSerializer.Deserialize<List<string>>(entity.LockedFieldsJson) ?? [];
            var configuration = JsonSerializer.Deserialize<JsonElement>(entity.ConfigurationJson);

            return new WorkflowTemplate
            {
                Id = entity.Id,
                Name = entity.Name,
                WorkflowTypeAlias = entity.WorkflowTypeAlias,
                Configuration = configuration,
                LockedFields = lockedFields,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt
            };
        }
    }
}
