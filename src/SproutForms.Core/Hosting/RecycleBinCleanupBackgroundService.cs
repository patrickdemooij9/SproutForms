using Microsoft.Extensions.Logging;
using SproutForms.Core.Services;

namespace SproutForms.Core.Hosting
{
    public class RecycleBinCleanupBackgroundService : PeriodicBackgroundService
    {
        private readonly RecycleBinCleanupService _cleanupService;

        public RecycleBinCleanupBackgroundService(RecycleBinCleanupService cleanupService, ILogger<RecycleBinCleanupBackgroundService> logger)
            : base(logger)
        {
            _cleanupService = cleanupService;
        }

        protected override TimeSpan Delay => TimeSpan.FromMinutes(1);
        protected override TimeSpan Period => TimeSpan.FromHours(1);

        protected override Task RunAsync(CancellationToken stoppingToken) => _cleanupService.CleanUpAsync();
    }
}
