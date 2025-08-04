using OllamaFarmer.Server.Services.Background.Interfaces;

namespace OllamaFarmer.Server.Services.Background
{
    public class BackgroundTaskService
        : BackgroundService
    {
        public IBackgroundTaskQueue TaskQueue { get; }

        private readonly ILogger<BackgroundTaskService> _logger;
        private readonly int _bgThreadCount;

        public BackgroundTaskService(IBackgroundTaskQueue taskQueue,
            ILogger<BackgroundTaskService> logger,
            int bgThreadCount = 1)
        {
            if (taskQueue == null)
                throw new ArgumentNullException(nameof(taskQueue));
            if (logger == null)
                throw new ArgumentNullException(nameof(logger));
            if (bgThreadCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(bgThreadCount), "Background thread count must be greater than zero.");
            TaskQueue = taskQueue;
            _logger = logger;
            _bgThreadCount = bgThreadCount;
        }


        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation($"Queued Hosted Service is running.");
            await BackgroundProcessing(stoppingToken);
        }

        private async Task BackgroundProcessing(CancellationToken stoppingToken)
        {
            var tasks = new List<Task>();
            for (int i = 0; i < _bgThreadCount; i++)
            {
                tasks.Add(ExecuteQueuedTasksAsync(stoppingToken));
            }
            try
            {
                await Task.WhenAll(tasks);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Background processing was canceled.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while executing background tasks.");
            }
        }

        private async Task ExecuteQueuedTasksAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(5000, stoppingToken);
                if (stoppingToken.IsCancellationRequested)
                {
                    _logger.LogInformation("Background processing was canceled.");
                    break;
                }
                _logger.LogInformation("Queued Hosted Service is working.");
                var workItem = await TaskQueue.DequeueAsync(stoppingToken);
                if (workItem == null)
                {
                    _logger.LogWarning("No work item found in the queue.");
                    await Task.Delay(5000, stoppingToken);
                    continue;
                }

                try
                {
                    _logger.LogInformation("Executing work item: {WorkItem}", nameof(workItem));
                    await workItem(stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // Prevent throwing if stoppingToken was signaled
                    _logger.LogInformation("Work item execution was canceled: {WorkItem}", nameof(workItem));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred executing {WorkItem}.", nameof(workItem));
                }
                finally
                {
                    _logger.LogInformation("Work item completed: {WorkItem}", nameof(workItem));
                }

            }
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Queued Hosted Service is stopping.");

            await base.StopAsync(stoppingToken);
        }
    }
}
