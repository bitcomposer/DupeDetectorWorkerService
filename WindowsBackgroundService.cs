namespace DupeDetectorWorkerService
{
    public sealed class WindowsBackgroundService(
        FileHasherService fileHasherService,
        ILogger<WindowsBackgroundService> logger
        ) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("The DupeDetector Service running at: {time}", DateTimeOffset.Now);
                }

                if (stoppingToken.IsCancellationRequested)
                {
                    logger.LogInformation("The DupeDetector Service stop requested at: {time}", DateTimeOffset.Now);
                    fileHasherService.RequestStop();
                }

                fileHasherService.DoLoop();

                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}
