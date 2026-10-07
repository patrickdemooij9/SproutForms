using SproutForms.Core.Flows;
using Umbraco.Cms.Core.Sync;
using Umbraco.Cms.Infrastructure.BackgroundJobs;

namespace SproutForms.Umbraco.Core.Implementations
{
    public class WorkflowExecutionWorker : IRecurringBackgroundJob
    {
        private readonly PendingWorkflowProcessor _processor;

        public TimeSpan Period => TimeSpan.FromSeconds(10);
        public TimeSpan Delay => TimeSpan.FromSeconds(1);

        // One server sends, so a load balanced site doesn't send an email or call a webhook from every server. The runner's claim
        // still guards against two servers both being the scheduling one while the role moves
        public ServerRole[] ServerRoles => [ServerRole.Single, ServerRole.SchedulingPublisher];

        public event EventHandler PeriodChanged { add { } remove { } }

        public WorkflowExecutionWorker(PendingWorkflowProcessor processor)
        {
            _processor = processor;
        }

        public Task RunJobAsync() => _processor.ProcessAsync(CancellationToken.None);
    }
}
