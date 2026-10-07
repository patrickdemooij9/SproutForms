using Microsoft.Extensions.Logging;
using SproutForms.Core.Flows;

namespace SproutForms.Core.Hosting
{
    public class WorkflowExecutionBackgroundService : PeriodicBackgroundService
    {
        private readonly PendingWorkflowProcessor _processor;

        public WorkflowExecutionBackgroundService(PendingWorkflowProcessor processor, ILogger<WorkflowExecutionBackgroundService> logger)
            : base(logger)
        {
            _processor = processor;
        }

        protected override TimeSpan Delay => TimeSpan.FromSeconds(1);
        protected override TimeSpan Period => TimeSpan.FromSeconds(10);

        protected override Task RunAsync(CancellationToken stoppingToken) => _processor.ProcessAsync(stoppingToken);
    }
}
