using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SproutForms.Core.Hosting
{
    /// <summary>
    /// Runs a job on a schedule on a site without Umbraco. Every instance of the site runs it, so a load balanced site needs storage
    /// whose <see cref="Repositories.IWorkflowExecutionRepository.TrySaveExecution"/> works across servers.
    /// </summary>
    public abstract class PeriodicBackgroundService : BackgroundService
    {
        private readonly ILogger _logger;

        protected PeriodicBackgroundService(ILogger logger)
        {
            _logger = logger;
        }

        protected abstract TimeSpan Delay { get; }
        protected abstract TimeSpan Period { get; }

        protected abstract Task RunAsync(CancellationToken stoppingToken);

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                await Task.Delay(Delay, stoppingToken);

                using var timer = new PeriodicTimer(Period);
                do
                {
                    try
                    {
                        await RunAsync(stoppingToken);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        _logger.LogError(ex, "{Job} failed", GetType().Name);
                    }
                }
                while (await timer.WaitForNextTickAsync(stoppingToken));
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // The site is stopping
            }
        }
    }
}
