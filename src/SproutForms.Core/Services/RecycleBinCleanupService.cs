using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SproutForms.Core.Services
{
    /// <summary>
    /// Deletes what has been in the recycle bin longer than SproutForms:RecycleBin:RetentionDays. The host calls it on a schedule.
    /// </summary>
    public class RecycleBinCleanupService
    {
        private readonly FormRecycleBinService _formRecycleBinService;
        private readonly FormSubmissionRecycleBinService _submissionRecycleBinService;
        private readonly IOptionsMonitor<SproutFormsOptions> _options;
        private readonly ILogger<RecycleBinCleanupService> _logger;

        public RecycleBinCleanupService(FormRecycleBinService formRecycleBinService,
            FormSubmissionRecycleBinService submissionRecycleBinService,
            IOptionsMonitor<SproutFormsOptions> options,
            ILogger<RecycleBinCleanupService> logger)
        {
            _formRecycleBinService = formRecycleBinService;
            _submissionRecycleBinService = submissionRecycleBinService;
            _options = options;
            _logger = logger;
        }

        public async Task CleanUpAsync()
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

                var deletedSubmissions = await _submissionRecycleBinService.PurgeAsync(retention, SystemUser.Key);
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
