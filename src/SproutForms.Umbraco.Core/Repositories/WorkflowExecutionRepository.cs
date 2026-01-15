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
            var utcNow = DateTime.UtcNow;
            var entities = await scope.Database.FetchAsync<WorkflowExecutionEntity>(@"
    SELECT *
    FROM SproutForms_WorkflowExecutions AS w
    WHERE
        w.Status IN (@0, @1)
        AND (
            w.NextAttemptUtc IS NULL
            OR w.NextAttemptUtc <= @2
        )
        AND NOT EXISTS (
            SELECT 1
            FROM SproutForms_WorkflowExecutions AS prev
            WHERE
                prev.SubmissionId = w.SubmissionId
                AND prev.[Order] < w.[Order]
                AND prev.Status <> @3
        )
    ORDER BY
        w.CreatedUtc ASC
",
            [
                (int)WorkflowExecutionStatus.Pending,
                (int)WorkflowExecutionStatus.Retrying,
                utcNow,
                (int)WorkflowExecutionStatus.Succeeded
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
