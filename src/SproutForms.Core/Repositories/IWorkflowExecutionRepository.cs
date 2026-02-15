using SproutForms.Core.Models;
using SproutForms.Core.Models.Flows;

namespace SproutForms.Core.Repositories
{
    public interface IWorkflowExecutionRepository
    {
        Task EnqueueAsync(IEnumerable<FormWorkflow> workflows, FormSubmission submission);
        Task SaveExecution(WorkflowExecution execution);
        Task<WorkflowExecution[]> GetPendingExecutions(int take);
        Task<WorkflowExecution[]> GetBySubmissionId(Guid submissionId);
    }
}
