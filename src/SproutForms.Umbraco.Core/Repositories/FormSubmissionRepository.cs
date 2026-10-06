using NPoco;
using SproutForms.Core.Models;
using SproutForms.Core.Repositories;
using SproutForms.Umbraco.Core.Models.Database;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Infrastructure.Scoping;
using Umbraco.Extensions;

namespace SproutForms.Umbraco.Core.Repositories
{
    public class FormSubmissionRepository : IFormSubmissionRepository
    {
        // Keeps the parameters of an IN clause below the limit of every database provider
        private const int IdBatchSize = 500;

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
                ValuesJson = JsonSerializer.Serialize(submission.Values),
                PageUrl = submission.PageUrl,
                ResultsJson = submission.Results.Count == 0 ? null : JsonSerializer.Serialize(submission.Results),
                VariablesJson = submission.Variables.Count == 0 ? null : JsonSerializer.Serialize(submission.Variables)
            });
        }

        public void DeleteAllByForm(Guid formId)
        {
            using var scope = _scopeProvider.CreateScope(autoComplete: true);
            var entities = scope.Database.Fetch<FormSubmissionEntity>(ByForm(scope, formId));

            foreach (var entity in entities)
            {
                scope.Database.Delete(entity);
            }
        }

        public void Delete(IEnumerable<Guid> ids)
        {
            using var scope = _scopeProvider.CreateScope(autoComplete: true);
            foreach (var batch in ids.Distinct().Chunk(IdBatchSize))
            {
                scope.Database.Execute("DELETE FROM SproutForms_FormSubmissions WHERE Id IN (@0)", [batch]);
            }
        }

        public void MoveToRecycleBin(IEnumerable<Guid> ids, DateTime trashedAt, string trashedBy)
        {
            using var scope = _scopeProvider.CreateScope(autoComplete: true);
            foreach (var batch in ids.Distinct().Chunk(IdBatchSize))
            {
                scope.Database.Execute("UPDATE SproutForms_FormSubmissions SET TrashedAt = @0, TrashedBy = @1 WHERE Id IN (@2)", [trashedAt, trashedBy, batch]);
            }
        }

        public void RestoreFromRecycleBin(IEnumerable<Guid> ids)
        {
            using var scope = _scopeProvider.CreateScope(autoComplete: true);
            foreach (var batch in ids.Distinct().Chunk(IdBatchSize))
            {
                scope.Database.Execute("UPDATE SproutForms_FormSubmissions SET TrashedAt = NULL, TrashedBy = NULL WHERE Id IN (@0)", [batch]);
            }
        }

        public async Task<FormSubmission> Get(Guid id)
        {
            using var scope = _scopeProvider.CreateScope(autoComplete: true);
            var entity = await scope.Database.FirstAsync<FormSubmissionEntity>(scope.SqlContext.Sql()
                .SelectAll()
                .From<FormSubmissionEntity>()
                .Where<FormSubmissionEntity>(it => it.Id == id));
            return Map(entity);
        }

        public IReadOnlyList<FormSubmission> GetByForm(Guid formId, int skip, int take, out int totalCount)
        {
            using var scope = _scopeProvider.CreateScope(autoComplete: true);
            var entities = scope.Database.Fetch<FormSubmissionEntity>(ByForm(scope, formId)
                .Where<FormSubmissionEntity>(it => it.TrashedAt == null)
                .OrderByDescending<FormSubmissionEntity>(it => it.SubmittedAt));
            totalCount = entities.Count;
            return [.. entities.Skip(skip).Take(take).Select(Map)];
        }

        public IReadOnlyList<FormSubmission> GetTrashedByForm(Guid formId, int skip, int take, out int totalCount)
        {
            using var scope = _scopeProvider.CreateScope(autoComplete: true);
            var entities = scope.Database.Fetch<FormSubmissionEntity>(ByForm(scope, formId)
                .Where<FormSubmissionEntity>(it => it.TrashedAt != null)
                .OrderByDescending<FormSubmissionEntity>(it => it.TrashedAt));
            totalCount = entities.Count;
            return [.. entities.Skip(skip).Take(take).Select(Map)];
        }

        public IReadOnlyList<FormSubmission> GetAllByForm(Guid formId)
        {
            using var scope = _scopeProvider.CreateScope(autoComplete: true);
            return [.. scope.Database.Fetch<FormSubmissionEntity>(ByForm(scope, formId)).Select(Map)];
        }

        public IReadOnlyList<FormSubmission> GetByIds(Guid formId, IEnumerable<Guid> ids)
        {
            using var scope = _scopeProvider.CreateScope(autoComplete: true);
            var submissions = new List<FormSubmission>();
            foreach (var batch in ids.Distinct().Chunk(IdBatchSize))
            {
                submissions.AddRange(scope.Database.Fetch<FormSubmissionEntity>(ByForm(scope, formId)
                    .WhereIn<FormSubmissionEntity>(it => it.Id, batch)).Select(Map));
            }
            return submissions;
        }

        public IReadOnlyList<FormSubmission> GetTrashedBefore(DateTime threshold)
        {
            using var scope = _scopeProvider.CreateScope(autoComplete: true);
            return [.. scope.Database.Fetch<FormSubmissionEntity>(scope.SqlContext.Sql()
                .SelectAll()
                .From<FormSubmissionEntity>()
                .Where<FormSubmissionEntity>(it => it.TrashedAt != null && it.TrashedAt < threshold)).Select(Map)];
        }

        public int Count(Guid formId)
        {
            using var scope = _scopeProvider.CreateScope(autoComplete: true);
            return scope.Database.ExecuteScalar<int>(scope.SqlContext.Sql()
                .SelectCount()
                .From<FormSubmissionEntity>()
                .InnerJoin<FormVersionEntity>()
                .On<FormSubmissionEntity, FormVersionEntity>((submission, version) => submission.FormVersionId == version.Id)
                .Where<FormVersionEntity>(it => it.FormId == formId)
                .Where<FormSubmissionEntity>(it => it.TrashedAt == null));
        }

        // Also the versions of submissions in the recycle bin: they may be restored
        public IReadOnlyCollection<Guid> GetVersionIdsWithSubmissions(Guid formId)
        {
            using var scope = _scopeProvider.CreateScope(autoComplete: true);
            return scope.Database.Fetch<Guid>(scope.SqlContext.Sql()
                .SelectDistinct<FormSubmissionEntity>(it => it.FormVersionId)
                .From<FormSubmissionEntity>()
                .InnerJoin<FormVersionEntity>()
                .On<FormSubmissionEntity, FormVersionEntity>((submission, version) => submission.FormVersionId == version.Id)
                .Where<FormVersionEntity>(it => it.FormId == formId));
        }

        private static Sql<ISqlContext> ByForm(IScope scope, Guid formId)
        {
            return scope.SqlContext.Sql()
                .SelectAll()
                .From<FormSubmissionEntity>()
                .InnerJoin<FormVersionEntity>()
                .On<FormSubmissionEntity, FormVersionEntity>((submission, version) => submission.FormVersionId == version.Id)
                .Where<FormVersionEntity>(it => it.FormId == formId);
        }

        private static FormSubmission Map(FormSubmissionEntity entity) => new()
        {
            Id = entity.Id,
            FormVersionId = entity.FormVersionId,
            Values = JsonSerializer.Deserialize<IReadOnlyDictionary<string, JsonElement>>(entity.ValuesJson)!,
            IpAddress = entity.IpAddress,
            SubmittedAt = entity.SubmittedAt,
            PageUrl = entity.PageUrl,
            Results = DeserializeResults(entity.ResultsJson),
            Variables = DeserializeResults(entity.VariablesJson),
            // The database gives the value back without its kind
            TrashedAt = entity.TrashedAt is { } trashedAt ? DateTime.SpecifyKind(trashedAt, DateTimeKind.Utc) : null,
            TrashedBy = entity.TrashedBy
        };

        // Submissions saved before form types or calculations, or with nothing computed, have none
        private static IReadOnlyDictionary<string, JsonElement> DeserializeResults(string? resultsJson)
        {
            return resultsJson is null
                ? new Dictionary<string, JsonElement>()
                : JsonSerializer.Deserialize<IReadOnlyDictionary<string, JsonElement>>(resultsJson)!;
        }
    }
}
