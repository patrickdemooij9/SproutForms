using SproutForms.Core.Models;
using SproutForms.Core.Models.Flows;
using SproutForms.Core.Repositories;
using System.Text.Json;

namespace SproutForms.Core.Storage.InMemory
{
    public class InMemoryWorkflowExecutionRepository : IWorkflowExecutionRepository
    {
        // The same timeout as the database repository: an execution still Running after it was interrupted and is picked up again
        private static readonly TimeSpan StaleRunningTimeout = TimeSpan.FromMinutes(15);

        private readonly InMemoryStore _store;

        public InMemoryWorkflowExecutionRepository(InMemoryStore store)
        {
            _store = store;
        }

        public Task EnqueueAsync(IEnumerable<FormWorkflow> workflows, FormSubmission submission)
        {
            lock (_store.Lock)
            {
                foreach (var workflow in workflows)
                {
                    var execution = new WorkflowExecution
                    {
                        Id = Guid.NewGuid(),
                        SubmissionId = submission.Id,
                        WorkflowAlias = workflow.Alias,
                        WorkflowTypeAlias = workflow.WorkflowTypeAlias,
                        ConfigurationJson = JsonSerializer.Serialize(workflow.Configuration),
                        Order = workflow.Order,
                        Status = WorkflowExecutionStatus.Pending,
                        CreatedUtc = DateTime.UtcNow
                    };
                    _store.Executions[execution.Id] = execution;
                }
            }
            return Task.CompletedTask;
        }

        public Task SaveExecution(WorkflowExecution execution)
        {
            lock (_store.Lock)
            {
                _store.Executions[execution.Id] = Copy(execution);
            }
            return Task.CompletedTask;
        }

        public Task<bool> TrySaveExecution(WorkflowExecution execution, WorkflowExecutionStatus expectedStatus, int expectedAttemptCount)
        {
            lock (_store.Lock)
            {
                if (!_store.Executions.TryGetValue(execution.Id, out var stored)
                    || stored.Status != expectedStatus
                    || stored.AttemptCount != expectedAttemptCount)
                {
                    return Task.FromResult(false);
                }

                _store.Executions[execution.Id] = Copy(execution);
                return Task.FromResult(true);
            }
        }

        public Task<WorkflowExecution[]> GetPendingExecutions(int take)
        {
            var utcNow = DateTime.UtcNow;
            lock (_store.Lock)
            {
                var pending = _store.Executions.Values
                    .Where(it => IsDue(it, utcNow) && !WaitsForEarlierWorkflow(it) && !IsPaused(it))
                    .OrderBy(it => it.CreatedUtc)
                    .Take(take)
                    .Select(Copy)
                    .ToArray();
                return Task.FromResult(pending);
            }
        }

        public Task<WorkflowExecution[]> GetBySubmissionId(Guid submissionId)
        {
            lock (_store.Lock)
            {
                return Task.FromResult(_store.Executions.Values
                    .Where(it => it.SubmissionId == submissionId)
                    .OrderBy(it => it.Order)
                    .Select(Copy)
                    .ToArray());
            }
        }

        public Task DeleteAllByForm(Guid formId)
        {
            lock (_store.Lock)
            {
                var submissionIds = _store.Submissions.Values.Where(it => _store.GetFormId(it) == formId).Select(it => it.Id).ToHashSet();
                RemoveWhere(it => submissionIds.Contains(it.SubmissionId));
            }
            return Task.CompletedTask;
        }

        public Task DeleteBySubmissions(IEnumerable<Guid> submissionIds)
        {
            var ids = submissionIds.ToHashSet();
            lock (_store.Lock)
            {
                RemoveWhere(it => ids.Contains(it.SubmissionId));
            }
            return Task.CompletedTask;
        }

        private static bool IsDue(WorkflowExecution execution, DateTime utcNow)
        {
            return execution.Status switch
            {
                WorkflowExecutionStatus.Pending or WorkflowExecutionStatus.Retrying => execution.NextAttemptUtc is null || execution.NextAttemptUtc <= utcNow,
                WorkflowExecutionStatus.Running => execution.StartedUtc <= utcNow.Subtract(StaleRunningTimeout),
                _ => false
            };
        }

        // Call inside the lock. A submission's workflows run in order: one waits until every earlier one succeeded
        private bool WaitsForEarlierWorkflow(WorkflowExecution execution)
            => _store.Executions.Values.Any(it => it.SubmissionId == execution.SubmissionId
                && it.Order < execution.Order
                && it.Status != WorkflowExecutionStatus.Succeeded);

        // Call inside the lock. Paused while the submission or its form is in the recycle bin
        private bool IsPaused(WorkflowExecution execution)
        {
            if (!_store.Submissions.TryGetValue(execution.SubmissionId, out var submission)) return false;
            if (submission.IsTrashed) return true;

            var formId = _store.GetFormId(submission);
            return formId.HasValue && _store.Forms.TryGetValue(formId.Value, out var form) && form.IsTrashed;
        }

        // Call inside the lock
        private void RemoveWhere(Func<WorkflowExecution, bool> predicate)
        {
            foreach (var id in _store.Executions.Values.Where(predicate).Select(it => it.Id).ToList())
            {
                _store.Executions.Remove(id);
            }
        }

        // The stored executions are never handed out, so the runner changing its instance doesn't change what is stored
        private static WorkflowExecution Copy(WorkflowExecution execution) => new()
        {
            Id = execution.Id,
            SubmissionId = execution.SubmissionId,
            WorkflowAlias = execution.WorkflowAlias,
            WorkflowTypeAlias = execution.WorkflowTypeAlias,
            ConfigurationJson = execution.ConfigurationJson,
            Order = execution.Order,
            Status = execution.Status,
            AttemptCount = execution.AttemptCount,
            LastError = execution.LastError,
            CreatedUtc = execution.CreatedUtc,
            NextAttemptUtc = execution.NextAttemptUtc,
            StartedUtc = execution.StartedUtc,
            CompletedUtc = execution.CompletedUtc
        };
    }
}
