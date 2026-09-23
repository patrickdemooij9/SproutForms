using Microsoft.Extensions.Logging;
using SproutForms.Core.Models.Flows;
using SproutForms.Core.Repositories;
using Umbraco.Cms.Infrastructure.BackgroundJobs;

namespace SproutForms.Umbraco.Core.Implementations
{
    public class WorkflowExecutionWorker : IRecurringBackgroundJob
    {
        private readonly IWorkflowExecutionRepository _workflowExecutionRepository;
        private readonly IWorkflowRunner _workflowRunner;
        private readonly ILogger<WorkflowExecutionWorker> _logger;

        public TimeSpan Period => TimeSpan.FromSeconds(10);
        public TimeSpan Delay => TimeSpan.FromSeconds(1);

        public event EventHandler PeriodChanged { add { } remove { } }

        public WorkflowExecutionWorker(IWorkflowExecutionRepository workflowExecutionRepository,
            IWorkflowRunner workflowRunner,
            ILogger<WorkflowExecutionWorker> logger)
        {
            _workflowExecutionRepository = workflowExecutionRepository;
            _workflowRunner = workflowRunner;
            _logger = logger;
        }

        public async Task RunJobAsync()
        {
            WorkflowExecution[] pendingExecutions;
            try
            {
                pendingExecutions = await _workflowExecutionRepository.GetPendingExecutions(10);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not fetch pending workflow executions");
                return;
            }

            foreach (var execution in pendingExecutions)
            {
                try
                {
                    await _workflowRunner.ExecuteWorkflowAsync(execution, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Workflow execution {ExecutionId} ({WorkflowAlias}) failed", execution.Id, execution.WorkflowAlias);
                }
            }
        }
    }
}
