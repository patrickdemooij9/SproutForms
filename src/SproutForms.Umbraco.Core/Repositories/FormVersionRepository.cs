using SproutForms.Core.Models;
using SproutForms.Core.Repositories;
using SproutForms.Umbraco.Core.Models.Database;
using System.Text.Json;
using Umbraco.Cms.Infrastructure.Scoping;
using Umbraco.Extensions;
using System.Collections.Generic;
using SproutForms.Core.JsonConverters;
using System.Linq;
using SproutForms.Core.Models.Outcomes;
using SproutForms.Core.Models.Flows;

namespace SproutForms.Umbraco.Core.Repositories
{
    public class FormVersionRepository : IFormVersionRepository
    {
        private readonly IScopeProvider _scopeProvider;
        private readonly IEnumerable<IFormFieldType> _fieldTypes;
        private readonly IEnumerable<IFormSubmitOutcomeType> _outcomeTypes;
        private readonly IEnumerable<IFormWorkflowType> _workflowTypes;

        public FormVersionRepository(IScopeProvider scopeProvider, IEnumerable<IFormFieldType> fieldTypes, IEnumerable<IFormSubmitOutcomeType> outcomeTypes, IEnumerable<IFormWorkflowType> workflowTypes)
        {
            _scopeProvider = scopeProvider;
            _fieldTypes = fieldTypes;
            _outcomeTypes = outcomeTypes;
            _workflowTypes = workflowTypes;
        }

        public void Add(FormVersion version)
        {
            using var scope = _scopeProvider.CreateScope(autoComplete: true);
            var published = GetPublished(version.FormId);
            if (published != null && version.Status == FormStatus.Published)
            {
                scope.Database.Execute("UPDATE SproutForms_FormVersions SET Status = @Status WHERE Id = @Id",
                    new { Status = (int)published.Status, Id = published.Id });
            }

            var options = new JsonSerializerOptions();
            options.Converters.Add(new FormFieldJsonConverter(_fieldTypes));
            options.Converters.Add(new FormSubmitOutcomeJsonConverter(_outcomeTypes));
            options.Converters.Add(new FormWorkflowJsonConverter(_workflowTypes));

            scope.Database.Insert(new FormVersionEntity
            {
                Id = version.Id,
                FormId = version.FormId,
                Status = (int)version.Status,
                Version = version.Version,
                DefinitionJson = JsonSerializer.Serialize(version.Definition, options),
                DefinitionHash = version.DefinitionHash,
                CreatedBy = version.CreatedBy,
                CreatedAt = version.CreatedAt
            });
        }

        public void DeleteAllByForm(Guid formId)
        {
            using var scope = _scopeProvider.CreateScope(autoComplete: true);
            var versions = scope.Database.Fetch<FormVersionEntity>(scope.SqlContext.Sql().SelectAll().From<FormVersionEntity>().Where<FormVersionEntity>(it => it.FormId == formId));

            foreach(var version in versions)
            {
                scope.Database.Delete(version);
            }
        }

        public FormVersion? GetLatest(Guid formId)
        {
            using var scope = _scopeProvider.CreateScope(autoComplete: true);
            var entity = scope.Database.FirstOrDefault<FormVersionEntity>(scope.SqlContext.Sql()
                .SelectAll()
                .From<FormVersionEntity>()
                .Where<FormVersionEntity>(it => it.FormId == formId)
                .OrderByDescending<FormVersionEntity>(it => it.CreatedAt));
            if (entity is null)
                return null;

            var options = new JsonSerializerOptions();
            options.Converters.Add(new FormFieldJsonConverter(_fieldTypes));
            options.Converters.Add(new FormSubmitOutcomeJsonConverter(_outcomeTypes));
            options.Converters.Add(new FormWorkflowJsonConverter(_workflowTypes));

            return new FormVersion
            {
                Id = entity.Id,
                FormId = entity.FormId,
                Version = entity.Version,
                Status = (FormStatus)entity.Status,
                Definition = JsonSerializer.Deserialize<FormDefinition>(entity.DefinitionJson, options)!,
                DefinitionHash = entity.DefinitionHash,
                CreatedBy = entity.CreatedBy,
                CreatedAt = entity.CreatedAt
            };
        }

        public FormVersion? GetPublished(Guid formId)
        {
            using var scope = _scopeProvider.CreateScope(autoComplete: true);
            var entity = scope.Database.FirstOrDefault<FormVersionEntity>(scope.SqlContext.Sql()
                .SelectAll()
                .From<FormVersionEntity>()
                .Where<FormVersionEntity>(it => it.FormId == formId && it.Status == (int)FormStatus.Published));
            if (entity is null)
                return null;

            var options = new JsonSerializerOptions();
            options.Converters.Add(new FormFieldJsonConverter(_fieldTypes));
            options.Converters.Add(new FormSubmitOutcomeJsonConverter(_outcomeTypes));
            options.Converters.Add(new FormWorkflowJsonConverter(_workflowTypes));

            return new FormVersion
            {
                Id = entity.Id,
                FormId = entity.FormId,
                Version = entity.Version,
                Status = (FormStatus)entity.Status,
                Definition = JsonSerializer.Deserialize<FormDefinition>(entity.DefinitionJson, options)!,
                DefinitionHash = entity.DefinitionHash,
                CreatedBy = entity.CreatedBy,
                CreatedAt = entity.CreatedAt
            };
        }

        public FormVersion? Get(Guid formVersionId)
        {
            using var scope = _scopeProvider.CreateScope(autoComplete: true);
            var entity = scope.Database.FirstOrDefault<FormVersionEntity>(scope.SqlContext.Sql()
                .SelectAll()
                .From<FormVersionEntity>()
                .Where<FormVersionEntity>(it => it.Id == formVersionId));
            if (entity is null)
                return null;

            var options = new JsonSerializerOptions();
            options.Converters.Add(new FormFieldJsonConverter(_fieldTypes));
            options.Converters.Add(new FormSubmitOutcomeJsonConverter(_outcomeTypes));
            options.Converters.Add(new FormWorkflowJsonConverter(_workflowTypes));

            return new FormVersion
            {
                Id = entity.Id,
                FormId = entity.FormId,
                Version = entity.Version,
                Status = (FormStatus)entity.Status,
                Definition = JsonSerializer.Deserialize<FormDefinition>(entity.DefinitionJson, options)!,
                DefinitionHash = entity.DefinitionHash,
                CreatedBy = entity.CreatedBy,
                CreatedAt = entity.CreatedAt
            };
        }

        public void Publish(Guid versionId)
        {
            throw new NotImplementedException();
        }
    }
}
