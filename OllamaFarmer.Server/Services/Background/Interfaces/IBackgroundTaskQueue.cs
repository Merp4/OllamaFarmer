using OllamaFarmer.Server.Dto.Sys;

namespace OllamaFarmer.Server.Services.Background.Interfaces
{
    // based on https://learn.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services?view=aspnetcore-3.1&tabs=visual-studio#queued-background-tasks



    public interface IBackgroundTaskQueue
    {
        int QueuedCount { get; }
        ValueTask EnqueueAsync(Func<CancellationToken, ValueTask> workItem, string name = "Background Task", string description = "A background task in the queue");
        ValueTask<Func<CancellationToken, ValueTask>> DequeueAsync(CancellationToken cancellationToken);
        Task ClearAsync();
        Task<TaskQueueDto> DescribeQueueAsync();
        Task<List<TaskQueueItemDto>> DescribeQueueItemsAsync();
        Task<TaskQueueItemDto> DescribeQueueItemAsync(Guid id);
        Task<TaskQueueItemDto> DescribeRunningQueueItemAsync();
    }

}
