using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SproutForms.Core.Models.Flows;
using SproutForms.Core.Repositories;

namespace SproutForms.Core.Flows
{
    public class WorkflowExecutionWorker : BackgroundService //TODO: Replace with the Umbraco specific handler
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IWorkflowExecutionRepository _workflowExecutionRepository;
        private readonly IWorkflowRunner _workflowRunner;

        public WorkflowExecutionWorker(IServiceScopeFactory scopeFactory,
            IWorkflowExecutionRepository workflowExecutionRepository,
            IWorkflowRunner workflowRunner)
        {
            _scopeFactory = scopeFactory;
            _workflowExecutionRepository = workflowExecutionRepository;
            _workflowRunner = workflowRunner;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            while (!stoppingToken.IsCancellationRequested)
            {
                await ProcessPendingAsync(stoppingToken);
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }

        private async Task ProcessPendingAsync(CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var pendingExecutions = await _workflowExecutionRepository.GetPendingExecutions(10);

            ExecutionContext.SuppressFlow();
            foreach (var execution in pendingExecutions)
            {
                await _workflowRunner.ExecuteWorkflowAsync(execution, ct);
            }
            ExecutionContext.RestoreFlow();
        }
    }
}
