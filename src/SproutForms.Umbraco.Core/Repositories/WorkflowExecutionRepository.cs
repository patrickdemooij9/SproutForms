using NPoco;
using SproutForms.Core.Models;
using SproutForms.Core.Models.Flows;
using SproutForms.Core.Repositories;
using SproutForms.Umbraco.Core.Models.Database;
using System.Text.Json;
using Umbraco.Cms.Infrastructure.Scoping;
using Umbraco.Extensions;

namespace SproutForms.Umbraco.Core.Repositories
{
    public class WorkflowExecutionRepository : IWorkflowExecutionRepository
    {
        private static readonly TimeSpan StaleRunningTimeout = TimeSpan.FromMinutes(15);

        private readonly IScopeProvider _scopeProvider;

        public WorkflowExecutionRepository(IScopeProvider scopeProvider)
        {
            _scopeProvider = scopeProvider;
        }

        public async Task EnqueueAsync(IEnumerable<FormWorkflow> workflows, FormSubmission submission)
        {
            using var scope = _scopeProvider.CreateScope(autoComplete: true);
            foreach (var workflow in workflows)
            {
                await scope.Database.InsertAsync(new WorkflowExecutionEntity
                {
                    Id = Guid.NewGuid(),
                    SubmissionId = submission.Id,
                    WorkflowAlias = workflow.Alias,
                    WorkflowTypeAlias = workflow.WorkflowTypeAlias,
                    ConfigurationJson = JsonSerializer.Serialize(workflow.Configuration),
                    Order = workflow.Order,
                    Status = (int)WorkflowExecutionStatus.Pending,
                    CreatedUtc = DateTime.UtcNow
                });
            }
        }

        public async Task<WorkflowExecution[]> GetPendingExecutions(int take)
        {
            using var scope = _scopeProvider.CreateScope(autoComplete: true);

            // I am not sure how to change this to an NPOCO in code query, so using raw SQL for now.
            // Executions still Running after the timeout were interrupted (e.g. an app restart) and are picked up again.
            var utcNow = DateTime.UtcNow;
            var entities = await scope.Database.FetchAsync<WorkflowExecutionEntity>(@"
    SELECT *
    FROM SproutForms_WorkflowExecutions AS w
    WHERE
        (
            (
                w.Status IN (@0, @1)
                AND (
                    w.NextAttemptUtc IS NULL
                    OR w.NextAttemptUtc <= @2
                )
            )
            OR (
                w.Status = @4
                AND w.StartedUtc <= @5
            )
        )
        AND NOT EXISTS (
            SELECT 1
            FROM SproutForms_WorkflowExecutions AS prev
            WHERE
                prev.SubmissionId = w.SubmissionId
                AND prev.[Order] < w.[Order]
                AND prev.Status <> @3
        )
        -- Paused while the submission or its form is in the recycle bin, and picked up again when it is restored
        AND NOT EXISTS (
            SELECT 1
            FROM SproutForms_FormSubmissions AS s
            INNER JOIN SproutForms_FormVersions AS v ON v.Id = s.FormVersionId
            INNER JOIN SproutForms_Forms AS f ON f.Id = v.FormId
            WHERE
                s.Id = w.SubmissionId
                AND (s.TrashedAt IS NOT NULL OR f.TrashedAt IS NOT NULL)
        )
    ORDER BY
        w.CreatedUtc ASC
",
            [
                (int)WorkflowExecutionStatus.Pending,
                (int)WorkflowExecutionStatus.Retrying,
                utcNow,
                (int)WorkflowExecutionStatus.Succeeded,
                (int)WorkflowExecutionStatus.Running,
                utcNow.Subtract(StaleRunningTimeout)
            ]
            );

            return entities
            .Take(take)
            .Select(e => new WorkflowExecution
            {
                Id = e.Id,
                SubmissionId = e.SubmissionId,
                WorkflowAlias = e.WorkflowAlias,
                WorkflowTypeAlias = e.WorkflowTypeAlias,
                ConfigurationJson = e.ConfigurationJson,
                Order = e.Order,
                Status = (WorkflowExecutionStatus)e.Status,
                AttemptCount = e.AttemptCount,
                LastError = e.LastError,
                CreatedUtc = e.CreatedUtc,
                NextAttemptUtc = e.NextAttemptUtc,
                StartedUtc = e.StartedUtc,
                CompletedUtc = e.CompletedUtc
            })
            .ToArray();
        }

        public async Task SaveExecution(WorkflowExecution execution)
        {
            using var scope = _scopeProvider.CreateScope(autoComplete: true);
            await scope.Database.SaveAsync(ToEntity(execution));
        }

        public async Task<bool> TrySaveExecution(WorkflowExecution execution, WorkflowExecutionStatus expectedStatus, int expectedAttemptCount)
        {
            using var scope = _scopeProvider.CreateScope(autoComplete: true);
            var updated = await scope.Database.ExecuteAsync(@"
    UPDATE SproutForms_WorkflowExecutions
    SET
        Status = @0,
        AttemptCount = @1,
        LastError = @2,
        NextAttemptUtc = @3,
        StartedUtc = @4,
        CompletedUtc = @5
    WHERE
        Id = @6
        AND Status = @7
        AND AttemptCount = @8",
            [
                (int)execution.Status,
                execution.AttemptCount,
                execution.LastError,
                execution.NextAttemptUtc,
                execution.StartedUtc,
                execution.CompletedUtc,
                execution.Id,
                (int)expectedStatus,
                expectedAttemptCount
            ]);
            return updated == 1;
        }

        public async Task<WorkflowExecution[]> GetBySubmissionId(Guid submissionId)
        {
            using var scope = _scopeProvider.CreateScope(autoComplete: true);

            var entities = await scope.Database.FetchAsync<WorkflowExecutionEntity>(scope.SqlContext.Sql()
                .SelectAll()
                .From<WorkflowExecutionEntity>()
                .Where<WorkflowExecutionEntity>(it => it.SubmissionId == submissionId)
                .OrderBy<WorkflowExecutionEntity>(it => it.Order));

            return entities
                .Select(e => new WorkflowExecution
                {
                    Id = e.Id,
                    SubmissionId = e.SubmissionId,
                    WorkflowAlias = e.WorkflowAlias,
                    WorkflowTypeAlias = e.WorkflowTypeAlias,
                    ConfigurationJson = e.ConfigurationJson,
                    Order = e.Order,
                    Status = (WorkflowExecutionStatus)e.Status,
                    AttemptCount = e.AttemptCount,
                    LastError = e.LastError,
                    CreatedUtc = e.CreatedUtc,
                    NextAttemptUtc = e.NextAttemptUtc,
                    StartedUtc = e.StartedUtc,
                    CompletedUtc = e.CompletedUtc
                })
                .ToArray();
        }

        public async Task DeleteAllByForm(Guid formId)
        {
            using var scope = _scopeProvider.CreateScope(autoComplete: true);
            await scope.Database.ExecuteAsync(@"
    DELETE FROM SproutForms_WorkflowExecutions
    WHERE SubmissionId IN (
        SELECT s.Id
        FROM SproutForms_FormSubmissions AS s
        INNER JOIN SproutForms_FormVersions AS v ON v.Id = s.FormVersionId
        WHERE v.FormId = @0
    )", formId);
        }

        public async Task DeleteBySubmissions(IEnumerable<Guid> submissionIds)
        {
            using var scope = _scopeProvider.CreateScope(autoComplete: true);
            // In batches, to stay below the parameter limit of every database provider
            foreach (var batch in submissionIds.Distinct().Chunk(500))
            {
                await scope.Database.ExecuteAsync("DELETE FROM SproutForms_WorkflowExecutions WHERE SubmissionId IN (@0)", [batch]);
            }
        }

        private WorkflowExecutionEntity ToEntity(WorkflowExecution execution)
        {
            return new WorkflowExecutionEntity
            {
                Id = execution.Id,
                SubmissionId = execution.SubmissionId,
                WorkflowAlias = execution.WorkflowAlias,
                WorkflowTypeAlias = execution.WorkflowTypeAlias,
                ConfigurationJson = execution.ConfigurationJson,
                Order = execution.Order,
                Status = (int)execution.Status,
                AttemptCount = execution.AttemptCount,
                LastError = execution.LastError,
                CreatedUtc = execution.CreatedUtc,
                NextAttemptUtc = execution.NextAttemptUtc,
                StartedUtc = execution.StartedUtc,
                CompletedUtc = execution.CompletedUtc
            };
        }
    }
}
