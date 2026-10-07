using SproutForms.Core.Models;
using SproutForms.Core.Models.Flows;
using SproutForms.Core.Repositories;
using System.Text.Json;

namespace SproutForms.Core.Flows
{
    public class WorkflowRunner : IWorkflowRunner
    {
        private const int MaxAttempts = 5;

        private readonly IFormWorkflowType[] _formWorkflowTypes;
        private readonly IWorkflowExecutionRepository _workflowExecutionRepository;
        private readonly IFormVersionRepository _formVersionRepository;
        private readonly IFormSubmissionRepository _formSubmissionRepository;

        public WorkflowRunner(IWorkflowExecutionRepository workflowExecutionRepository,
            IFormVersionRepository formVersionRepository,
            IFormSubmissionRepository formSubmissionRepository, IEnumerable<IFormWorkflowType> formWorkflowTypes)
        {
            _formWorkflowTypes = formWorkflowTypes.ToArray();
            _workflowExecutionRepository = workflowExecutionRepository;
            _formVersionRepository = formVersionRepository;
            _formSubmissionRepository = formSubmissionRepository;
        }

        public async Task<bool> ExecuteWorkflowAsync(WorkflowExecution execution, CancellationToken ct)
        {
            var (fetchedStatus, fetchedAttemptCount) = (execution.Status, execution.AttemptCount);
            execution.Status = WorkflowExecutionStatus.Running;
            execution.StartedUtc = DateTime.UtcNow;
            execution.AttemptCount++;

            // Claimed only while nothing changed it since it was fetched; otherwise another server, or a manual retry, has it
            if (!await _workflowExecutionRepository.TrySaveExecution(execution, fetchedStatus, fetchedAttemptCount))
                return false;

            try
            {
                var workflowType = _formWorkflowTypes.FirstOrDefault(wt => wt.Alias == execution.WorkflowTypeAlias) ?? throw new Exception($"Could not find workflow type with alias {execution.WorkflowTypeAlias}");
                var submission = await _formSubmissionRepository.Get(execution.SubmissionId);
                var version = _formVersionRepository.Get(submission.FormVersionId);

                var result = await workflowType.ExecuteAsync(new WorkflowContext
                {
                    Workflow = new FormWorkflow
                    {
                        Alias = execution.WorkflowAlias,
                        WorkflowTypeAlias = execution.WorkflowTypeAlias,
                        Configuration = JsonSerializer.Deserialize(execution.ConfigurationJson, workflowType.ConfigurationType)!,
                    },
                    Submission = submission!,
                    Version = version!
                }, ct);

                if (result.Success)
                {
                    execution.Status = WorkflowExecutionStatus.Succeeded;
                    execution.CompletedUtc = DateTime.UtcNow;
                    execution.LastError = null;
                }
                else if (result.Retryable && execution.AttemptCount < MaxAttempts)
                {
                    execution.Status = WorkflowExecutionStatus.Retrying;
                    execution.NextAttemptUtc = DateTime.UtcNow.Add(GetRetryDelay(execution.AttemptCount));
                    execution.LastError = result.Error;
                }
                else
                {
                    execution.Status = WorkflowExecutionStatus.Failed;
                    execution.CompletedUtc = DateTime.UtcNow;
                    execution.LastError = result.Error;
                }
            }
            catch (Exception ex)
            {
                execution.Status = WorkflowExecutionStatus.Failed;
                execution.CompletedUtc = DateTime.UtcNow;
                execution.LastError = ex.Message;
            }

            await _workflowExecutionRepository.SaveExecution(execution);
            return true;
        }

        public async Task<WorkflowRetryResult> RetryAsync(Guid submissionId, string workflowAlias)
        {
            var executions = await _workflowExecutionRepository.GetBySubmissionId(submissionId);
            var execution = executions.FirstOrDefault(e => e.WorkflowAlias == workflowAlias);
            if (execution is null)
                return WorkflowRetryResult.NotFound;

            // Pending, Running and Succeeded executions are already queued, in progress or done
            if (execution.Status is not (WorkflowExecutionStatus.Failed or WorkflowExecutionStatus.Retrying))
                return WorkflowRetryResult.NotRetryable;

            // A manual retry starts a fresh series of attempts; LastError stays until the next run replaces it. When the worker started a
            // scheduled retry in the meantime, it is already running
            var (fetchedStatus, fetchedAttemptCount) = (execution.Status, execution.AttemptCount);
            execution.Status = WorkflowExecutionStatus.Pending;
            execution.AttemptCount = 0;
            execution.NextAttemptUtc = null;
            execution.CompletedUtc = null;
            if (!await _workflowExecutionRepository.TrySaveExecution(execution, fetchedStatus, fetchedAttemptCount))
                return WorkflowRetryResult.NotRetryable;

            return WorkflowRetryResult.Queued;
        }

        // 1, 2, 4, 8 minutes between attempts
        private static TimeSpan GetRetryDelay(int attemptCount)
        {
            return TimeSpan.FromMinutes(Math.Pow(2, attemptCount - 1));
        }
    }
}
