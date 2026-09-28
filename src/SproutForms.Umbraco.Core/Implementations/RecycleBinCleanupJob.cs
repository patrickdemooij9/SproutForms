using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SproutForms.Core;
using SproutForms.Core.Services;
using SproutForms.Umbraco.Core.Services;
using Umbraco.Cms.Core.Sync;
using Umbraco.Cms.Infrastructure.BackgroundJobs;

namespace SproutForms.Umbraco.Core.Implementations
{
    /// <summary>
    /// Deletes what has been in the recycle bin longer than SproutForms:RecycleBin:RetentionDays.
    /// </summary>
    public class RecycleBinCleanupJob : IRecurringBackgroundJob
    {
        private readonly FormRecycleBinService _formRecycleBinService;
        private readonly FormSubmissionRecycleBinService _submissionRecycleBinService;
        private readonly IOptionsMonitor<SproutFormsOptions> _options;
        private readonly ILogger<RecycleBinCleanupJob> _logger;

        public TimeSpan Period => TimeSpan.FromHours(1);
        public TimeSpan Delay => TimeSpan.FromMinutes(1);

        // One server cleans up, so a load balanced site doesn't delete the same form twice
        public ServerRole[] ServerRoles => [ServerRole.Single, ServerRole.SchedulingPublisher];

        public event EventHandler PeriodChanged { add { } remove { } }

        public RecycleBinCleanupJob(FormRecycleBinService formRecycleBinService,
            FormSubmissionRecycleBinService submissionRecycleBinService,
            IOptionsMonitor<SproutFormsOptions> options,
            ILogger<RecycleBinCleanupJob> logger)
        {
            _formRecycleBinService = formRecycleBinService;
            _submissionRecycleBinService = submissionRecycleBinService;
            _options = options;
            _logger = logger;
        }

        public async Task RunJobAsync()
        {
            var retentionDays = _options.CurrentValue.RecycleBin.RetentionDays;
            if (retentionDays <= 0) return;

            var retention = TimeSpan.FromDays(retentionDays);
            try
            {
                var deletedForms = await _formRecycleBinService.PurgeAsync(retention);
                if (deletedForms > 0)
                {
                    _logger.LogInformation("Deleted {Count} forms that were in the recycle bin for more than {Days} days", deletedForms, retentionDays);
                }

                var deletedSubmissions = await _submissionRecycleBinService.PurgeAsync(retention, BackofficeUserNameResolver.SystemUserKey);
                if (deletedSubmissions > 0)
                {
                    _logger.LogInformation("Deleted {Count} submissions that were in the recycle bin for more than {Days} days", deletedSubmissions, retentionDays);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not clean up the recycle bin");
            }
        }
    }
}
