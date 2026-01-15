using SproutForms.Core.Models;
using SproutForms.Core.Repositories;
using SproutForms.Umbraco.Core.Models.Database;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using Umbraco.Cms.Infrastructure.Scoping;
using Umbraco.Extensions;

namespace SproutForms.Umbraco.Core.Repositories
{
    public class FormSubmissionRepository : IFormSubmissionRepository
    {
        private readonly IScopeProvider _scopeProvider;

        public FormSubmissionRepository(IScopeProvider scopeProvider)
        {
            _scopeProvider = scopeProvider;
        }

        public void Add(FormSubmission submission)
        {
            using var scope = _scopeProvider.CreateScope(autoComplete: true);
            scope.Database.Insert(new FormSubmissionEntity
            {
                Id = submission.Id,
                FormVersionId = submission.FormVersionId,
                IpAddress = submission.IpAddress,
                SubmittedAt = submission.SubmittedAt,
                ValuesJson = JsonSerializer.Serialize(submission.Values)
            });
        }

        public void DeleteAllByForm(Guid formId)
        {
            using var scope = _scopeProvider.CreateScope(autoComplete: true);
            var entities = scope.Database.Fetch<FormSubmissionEntity>(scope.SqlContext.Sql()
                .SelectAll()
                .From<FormSubmissionEntity>()
                .InnerJoin<FormVersionEntity>()
                .On<FormSubmissionEntity, FormVersionEntity>((submission, version) => submission.FormVersionId == version.Id)
                .Where<FormVersionEntity>(it => it.FormId == formId));

            foreach (var entity in entities)
            {
                scope.Database.Delete(entity);
            }
        }

        public async Task<FormSubmission> Get(Guid id)
        {
            using var scope = _scopeProvider.CreateScope(autoComplete: true);
            var entity = await scope.Database.FirstAsync<FormSubmissionEntity>(scope.SqlContext.Sql()
                .SelectAll()
                .From<FormSubmissionEntity>()
                .Where<FormSubmissionEntity>(it => it.Id == id));
            return new FormSubmission
            {
                Id = entity.Id,
                FormVersionId = entity.FormVersionId,
                Values = JsonSerializer.Deserialize<IReadOnlyDictionary<string, JsonElement>>(entity.ValuesJson),
                IpAddress = entity.IpAddress,
                SubmittedAt = entity.SubmittedAt
            };
        }

        public IReadOnlyList<FormSubmission> GetByForm(Guid formId, int skip, int take, out int totalCount)
        {
            using var scope = _scopeProvider.CreateScope(autoComplete: true);
            var entities = scope.Database.Fetch<FormSubmissionEntity>(scope.SqlContext.Sql()
                .SelectAll()
                .From<FormSubmissionEntity>()
                .InnerJoin<FormVersionEntity>()
                .On<FormSubmissionEntity, FormVersionEntity>((submission, version) => submission.FormVersionId == version.Id)
                .Where<FormVersionEntity>(it => it.FormId == formId)
                .OrderByDescending<FormSubmissionEntity>(it => it.SubmittedAt));
            totalCount = entities.Count;
            var submissions = new List<FormSubmission>();
            foreach (var entity in entities.Skip(skip).Take(take))
            {
                submissions.Add(new FormSubmission
                {
                    Id = entity.Id,
                    FormVersionId = entity.FormVersionId,
                    Values = JsonSerializer.Deserialize<IReadOnlyDictionary<string, JsonElement>>(entity.ValuesJson),
                    IpAddress = entity.IpAddress,
                    SubmittedAt = entity.SubmittedAt
                });
            }
            return submissions;
        }

        public int Count(Guid formId)
        {
            using var scope = _scopeProvider.CreateScope(autoComplete: true);
            return scope.Database.ExecuteScalar<int>(scope.SqlContext.Sql()
                .SelectCount()
                .From<FormSubmissionEntity>()
                .InnerJoin<FormVersionEntity>()
                .On<FormSubmissionEntity, FormVersionEntity>((submission, version) => submission.FormVersionId == version.Id)
                .Where<FormVersionEntity>(it => it.FormId == formId));
        }
    }
}
