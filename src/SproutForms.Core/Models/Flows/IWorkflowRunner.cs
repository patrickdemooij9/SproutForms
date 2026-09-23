namespace SproutForms.Core.Models.Flows
{
    public interface IWorkflowRunner
    {
        Task ExecuteWorkflowAsync(WorkflowExecution execution, CancellationToken ct);

        /// <summary>
        /// Queues a failed workflow of a submission to run again; the background worker picks it up on its next run.
        /// </summary>
        Task<WorkflowRetryResult> RetryAsync(Guid submissionId, string workflowAlias);
    }

    public enum WorkflowRetryResult
    {
        Queued,
        NotFound,
        NotRetryable
    }
}
