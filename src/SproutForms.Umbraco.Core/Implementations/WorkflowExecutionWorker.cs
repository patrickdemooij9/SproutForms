using Microsoft.Extensions.DependencyInjection;
using SproutForms.Core.Models.Flows;
using SproutForms.Core.Repositories;
using System;
using System.Collections.Generic;
using System.Text;
using Umbraco.Cms.Infrastructure.BackgroundJobs;

namespace SproutForms.Umbraco.Core.Implementations
{
    public class WorkflowExecutionWorker : IRecurringBackgroundJob
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IWorkflowExecutionRepository _workflowExecutionRepository;
        private readonly IWorkflowRunner _workflowRunner;

        public TimeSpan Period => TimeSpan.FromSeconds(10);
        public TimeSpan Delay => TimeSpan.FromSeconds(1);

        public event EventHandler PeriodChanged { add { } remove { } }

        public WorkflowExecutionWorker(IServiceScopeFactory scopeFactory,
            IWorkflowExecutionRepository workflowExecutionRepository,
            IWorkflowRunner workflowRunner)
        {
            _scopeFactory = scopeFactory;
            _workflowExecutionRepository = workflowExecutionRepository;
            _workflowRunner = workflowRunner;
        }

        public async Task RunJobAsync()
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var pendingExecutions = await _workflowExecutionRepository.GetPendingExecutions(10);

                ExecutionContext.SuppressFlow();
                foreach (var execution in pendingExecutions)
                {
                    await _workflowRunner.ExecuteWorkflowAsync(execution, CancellationToken.None);
                }
                ExecutionContext.RestoreFlow();
            }
            catch(Exception ex)
            {
                // Log exception
            }   
        }
    }
}
