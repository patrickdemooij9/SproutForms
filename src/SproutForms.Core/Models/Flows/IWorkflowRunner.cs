namespace SproutForms.Core.Models.Flows
{
    public interface IWorkflowRunner
    {
        Task ExecuteWorkflowAsync(WorkflowExecution execution, CancellationToken ct);
    }
}
