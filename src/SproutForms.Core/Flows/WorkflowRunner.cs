using SproutForms.Core.Models;
using SproutForms.Core.Models.Flows;
using SproutForms.Core.Repositories;
using System.Text.Json;

namespace SproutForms.Core.Flows
{
    public class WorkflowRunner : IWorkflowRunner
    {
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

        public async Task ExecuteWorkflowAsync(WorkflowExecution execution, CancellationToken ct)
        {
            execution.Status = WorkflowExecutionStatus.Running;
            execution.StartedUtc = DateTime.UtcNow;
            execution.AttemptCount++;

            await _workflowExecutionRepository.SaveExecution(execution);

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
                else if (result.Retryable)
                {
                    execution.Status = WorkflowExecutionStatus.Retrying;
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
                execution.LastError = ex.Message;
            }

            await _workflowExecutionRepository.SaveExecution(execution);
        }
    }
}
