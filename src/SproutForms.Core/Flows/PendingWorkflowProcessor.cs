using Microsoft.Extensions.Logging;
using SproutForms.Core.Models.Flows;
using SproutForms.Core.Repositories;

namespace SproutForms.Core.Flows
{
    /// <summary>
    /// Runs the workflow executions that are due. The host calls it on a schedule.
    /// </summary>
    public class PendingWorkflowProcessor
    {
        private const int BatchSize = 10;

        private readonly IWorkflowExecutionRepository _workflowExecutionRepository;
        private readonly IWorkflowRunner _workflowRunner;
        private readonly ILogger<PendingWorkflowProcessor> _logger;

        public PendingWorkflowProcessor(IWorkflowExecutionRepository workflowExecutionRepository,
            IWorkflowRunner workflowRunner,
            ILogger<PendingWorkflowProcessor> logger)
        {
            _workflowExecutionRepository = workflowExecutionRepository;
            _workflowRunner = workflowRunner;
            _logger = logger;
        }

        public async Task ProcessAsync(CancellationToken cancellationToken)
        {
            WorkflowExecution[] pendingExecutions;
            try
            {
                pendingExecutions = await _workflowExecutionRepository.GetPendingExecutions(BatchSize);
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
                    if (!await _workflowRunner.ExecuteWorkflowAsync(execution, cancellationToken))
                        _logger.LogDebug("Workflow execution {ExecutionId} ({WorkflowAlias}) was claimed elsewhere", execution.Id, execution.WorkflowAlias);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Workflow execution {ExecutionId} ({WorkflowAlias}) failed", execution.Id, execution.WorkflowAlias);
                }
            }
        }
    }
}
