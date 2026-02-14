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
using Umbraco.Cms.Core.Cache;
using SproutForms.Umbraco.Core.Caching;

namespace SproutForms.Umbraco.Core.Repositories
{
    public class FormVersionRepository : IFormVersionRepository
    {
        private readonly IScopeProvider _scopeProvider;
        private readonly IEnumerable<IFormFieldType> _fieldTypes;
        private readonly IEnumerable<IFormSubmitOutcomeType> _outcomeTypes;
        private readonly IEnumerable<IFormWorkflowType> _workflowTypes;

        private readonly Caching.IRepositoryCachePolicy<FormVersion, Guid> _cachePolicy;

        public FormVersionRepository(IScopeProvider scopeProvider,
            IEnumerable<IFormFieldType> fieldTypes,
            IEnumerable<IFormSubmitOutcomeType> outcomeTypes,
            IEnumerable<IFormWorkflowType> workflowTypes,
            IAppPolicyCache cache)
        {
            _scopeProvider = scopeProvider;
            _fieldTypes = fieldTypes;
            _outcomeTypes = outcomeTypes;
            _workflowTypes = workflowTypes;

            _cachePolicy = new Caching.DefaultRepositoryCachePolicy<FormVersion, Guid>(cache, new RepositoryPolicyOptions<FormVersion, Guid>(it => it.Id));
        }

        public void Add(FormVersion version)
        {
            _cachePolicy.Create(version, DoAdd);
        }

        private void DoAdd(FormVersion version)
        {
            using var scope = _scopeProvider.CreateScope(autoComplete: true);
            var published = GetPublished(version.FormId);
            if (published != null && version.Status == FormStatus.Published)
            {
                scope.Database.Execute("UPDATE SproutForms_FormVersions SET Status = @Status WHERE Id = @Id",
                    new { Status = (int)FormStatus.Draft, Id = published.Id });
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
            var versions = _cachePolicy.GetByProperty(formId, DoGetByFormId, nameof(FormVersion.FormId));

            foreach (var version in versions)
            {
                _cachePolicy.Delete(version, DoDelete);
            }
        }

        private void DoDelete(FormVersion version)
        {
            using var scope = _scopeProvider.CreateScope(autoComplete: true);
            scope.Database.Delete<FormVersionEntity>(version);
        }

        public FormVersion? GetLatest(Guid formId)
        {
            return _cachePolicy.GetByProperty(formId, DoGetByFormId, nameof(FormVersion.FormId)).OrderByDescending(it => it.CreatedAt).FirstOrDefault();
        }

        public FormVersion? GetPublished(Guid formId)
        {
            return _cachePolicy.GetByProperty(formId, DoGetByFormId, nameof(FormVersion.FormId)).FirstOrDefault(it => it.Status == FormStatus.Published);
        }

        private FormVersion[] DoGetByFormId(Guid formId)
        {
            Console.WriteLine("Cache fail!");
            using var scope = _scopeProvider.CreateScope(autoComplete: true);
            var entities = scope.Database.Fetch<FormVersionEntity>(scope.SqlContext.Sql()
                .SelectAll()
                .From<FormVersionEntity>()
                .Where<FormVersionEntity>(it => it.FormId == formId));
            if (entities.Count == 0)
                return [];

            var options = new JsonSerializerOptions();
            options.Converters.Add(new FormFieldJsonConverter(_fieldTypes));
            options.Converters.Add(new FormSubmitOutcomeJsonConverter(_outcomeTypes));
            options.Converters.Add(new FormWorkflowJsonConverter(_workflowTypes));

            return [.. entities.Select(entity => new FormVersion
            {
                Id = entity.Id,
                FormId = entity.FormId,
                Version = entity.Version,
                Status = (FormStatus)entity.Status,
                Definition = JsonSerializer.Deserialize<FormDefinition>(entity.DefinitionJson, options)!,
                DefinitionHash = entity.DefinitionHash,
                CreatedBy = entity.CreatedBy,
                CreatedAt = entity.CreatedAt
            })];
        }

        public FormVersion? Get(Guid formVersionId)
        {
            return _cachePolicy.Get(formVersionId, DoGetById);
        }

        private FormVersion? DoGetById(Guid id)
        {
            Console.WriteLine("Cache fail!");
            using var scope = _scopeProvider.CreateScope(autoComplete: true);
            var entity = scope.Database.FirstOrDefault<FormVersionEntity>(scope.SqlContext.Sql()
                .SelectAll()
                .From<FormVersionEntity>()
                .Where<FormVersionEntity>(it => it.Id == id));
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
