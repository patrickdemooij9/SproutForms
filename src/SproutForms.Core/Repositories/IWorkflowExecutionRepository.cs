using SproutForms.Core.Models;
using SproutForms.Core.Models.Flows;

namespace SproutForms.Core.Repositories
{
    public interface IWorkflowExecutionRepository
    {
        Task EnqueueAsync(IEnumerable<FormWorkflow> workflows, FormSubmission submission);
        Task SaveExecution(WorkflowExecution execution);

        /// <summary>
        /// Saves the execution only when it still has the status and attempt count it was read with, and returns whether it did. Another
        /// server, or a manual retry, may have changed it since; the one that saves first wins, so it never runs twice at once.
        /// </summary>
        Task<bool> TrySaveExecution(WorkflowExecution execution, WorkflowExecutionStatus expectedStatus, int expectedAttemptCount);
        Task<WorkflowExecution[]> GetPendingExecutions(int take);
        Task<WorkflowExecution[]> GetBySubmissionId(Guid submissionId);
        Task DeleteAllByForm(Guid formId);
        Task DeleteBySubmissions(IEnumerable<Guid> submissionIds);
    }
}
