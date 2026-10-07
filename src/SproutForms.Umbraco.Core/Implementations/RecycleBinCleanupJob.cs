using SproutForms.Core.Services;
using Umbraco.Cms.Core.Sync;
using Umbraco.Cms.Infrastructure.BackgroundJobs;

namespace SproutForms.Umbraco.Core.Implementations
{
    /// <summary>
    /// Deletes what has been in the recycle bin longer than SproutForms:RecycleBin:RetentionDays.
    /// </summary>
    public class RecycleBinCleanupJob : IRecurringBackgroundJob
    {
        private readonly RecycleBinCleanupService _cleanupService;

        public TimeSpan Period => TimeSpan.FromHours(1);
        public TimeSpan Delay => TimeSpan.FromMinutes(1);

        // One server cleans up, so a load balanced site doesn't delete the same form twice
        public ServerRole[] ServerRoles => [ServerRole.Single, ServerRole.SchedulingPublisher];

        public event EventHandler PeriodChanged { add { } remove { } }

        public RecycleBinCleanupJob(RecycleBinCleanupService cleanupService)
        {
            _cleanupService = cleanupService;
        }

        public Task RunJobAsync() => _cleanupService.CleanUpAsync();
    }
}
