using OllamaFarmer.Server.Dto.Sys;
using OllamaFarmer.Server.Services.Background.Interfaces;
using System.Threading.Channels;

namespace OllamaFarmer.Server.Services.Background
{
    public class BackgroundTaskQueue : IBackgroundTaskQueue
    {
        private readonly Channel<TaskQueueItem> _queue;
        public int QueuedCount => _queue.Reader.CanCount ? _queue.Reader.Count : -1;

        public BackgroundTaskQueue(int capacity)
        {
            if (capacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be greater than zero.");

            // Capacity should be set based on the expected application load and
            // number of concurrent threads accessing the queue.            
            // BoundedChannelFullMode.Wait will cause calls to WriteAsync() to return a task,
            // which completes only when space became available. This leads to backpressure,
            // in case too many publishers/calls start accumulating.
            var options = new BoundedChannelOptions(capacity)
            {
                FullMode = BoundedChannelFullMode.Wait
            };
            _queue = Channel.CreateBounded<TaskQueueItem>(options);
        }

        public async ValueTask EnqueueAsync(
            Func<CancellationToken, ValueTask> workItem, 
            string name = "Background Task", string description = "A background task in the queue")
        {
            if (workItem == null)
                throw new ArgumentNullException(nameof(workItem));

            var item = new TaskQueueItem(workItem)
            {
                Name = name,
                Description = description
            };
            
            await _queue.Writer.WriteAsync(item);
        }

        public async ValueTask<Func<CancellationToken, ValueTask>> DequeueAsync(
            CancellationToken cancellationToken)
        {
            var workItem = await _queue.Reader.ReadAsync(cancellationToken);
            if (workItem == null)
                throw new InvalidOperationException("No work item available in the queue.");
            workItem.MarkAsRunning();
            return async (cancellationToken) =>
            {
                try
                {
                    await workItem.ExecuteAsync(cancellationToken);
                    workItem.MarkAsCompleted();
                }
                catch (Exception ex)
                {
                    workItem.MarkAsFailed(ex.Message);
                    throw; // Re-throw the exception after marking as failed
                }
                finally
                {
                    workItem.MarkAsNotRunning();
                }
            };
        }


        public async Task ClearAsync()
        {
            if (QueuedCount == 0)
                return; // Nothing to clear
                
            while (_queue.Reader.CanPeek && await _queue.Reader.WaitToReadAsync())
            {
                if (_queue.Reader.TryRead(out _))
                {
                    // Successfully read an item, continue to clear the queue
                }
            }
        }

        public Task<TaskQueueDto> DescribeQueueAsync()
        {
            return Task.FromResult(new TaskQueueDto
            {
                QueuedCount = QueuedCount
            });
        }

        public async Task<List<TaskQueueItemDto>> DescribeQueueItemsAsync()
        {
            var items = new List<TaskQueueItemDto>();
            if (_queue.Reader.Count == 0)
            {
                return items; // Return empty list if no items in the queue
            }
            
            while (await _queue.Reader.WaitToReadAsync())
            {
                if (_queue.Reader.TryRead(out var item))
                {
                    items.Add(new TaskQueueItemDto
                    {
                        Id = item.Id.ToString(),
                        Name = item.Name,
                        Description = item.Description,
                        CreatedAt = item.CreatedAt,
                        CompletedAt = item.CompletedAt,
                        IsCompleted = item.IsCompleted,
                        IsRunning = item.IsRunning,
                        IsFailed = item.IsFailed,
                        ErrorMessage = item.ErrorMessage
                    });
                }
            }
            return items;
        }

        public async Task<TaskQueueItemDto> DescribeQueueItemAsync(Guid id)
        {
            var items = await DescribeQueueItemsAsync();
            var item = items.FirstOrDefault(i => i.Id == id.ToString());
            if (item == null)
                throw new KeyNotFoundException($"No task found with ID {id}");
            return item;
        }

        public async Task<TaskQueueItemDto> DescribeRunningQueueItemAsync()
        {
            var items = await DescribeQueueItemsAsync();
            var runningItem = items.FirstOrDefault(i => i.IsRunning);
            if (runningItem == null)
                throw new InvalidOperationException("No running task found in the queue.");
            return runningItem;
        }
    }

    internal class TaskQueueItem
    {
        internal Func<CancellationToken, ValueTask> taskFunction { get; set; }
        
        public TaskQueueItem(Func<CancellationToken, ValueTask> func)
        {
            this.taskFunction = func ?? throw new ArgumentNullException(nameof(func));
        }

        public TaskQueueItem(Action<CancellationToken> action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            taskFunction = async cancellationToken =>
            {
                action(cancellationToken);
                await ValueTask.CompletedTask; // Ensure it matches the delegate type
            };
        }
        
        public TaskQueueItem(Func<CancellationToken, Task> func)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));
            this.taskFunction = async cancellationToken =>
            {
                await func(cancellationToken);
            };
        }

        public TaskQueueItem(Action<CancellationToken, Task> action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            taskFunction = async cancellationToken =>
            {
                action(cancellationToken, Task.CompletedTask);
                await ValueTask.CompletedTask; // Ensure it matches the delegate type
            };
        }
        
        public TaskQueueItem(Action action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            taskFunction = async cancellationToken =>
            {
                action();
                await ValueTask.CompletedTask; // Ensure it matches the delegate type
            };
        }
        
        public TaskQueueItem(Func<Task> func)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));
            this.taskFunction = async cancellationToken =>
            {
                await func();
            };
        }

        public async ValueTask ExecuteAsync(CancellationToken cancellationToken)
        {
            if (taskFunction == null)
                throw new InvalidOperationException("Work item function is not set.");
            await taskFunction(cancellationToken);
        }

        public Guid Id { get; set; } = Guid.NewGuid();
        public required string Name { get; set; }
        public string Description { get; set; } = "A background task in the queue";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; } = null;
        public bool IsCompleted { get; set; } = false;
        public bool IsRunning { get; set; } = false;
        public bool IsFailed { get; set; } = false;
        public string? ErrorMessage { get; set; } = null;
        
        public void MarkAsCompleted()
        {
            IsCompleted = true;
            CompletedAt = DateTime.UtcNow;
        }
        
        public void MarkAsFailed(string errorMessage)
        {
            IsFailed = true;
            ErrorMessage = errorMessage;
            CompletedAt = DateTime.UtcNow;
        }
        
        public void MarkAsRunning()
        {
            IsRunning = true;
            IsCompleted = false;
            IsFailed = false;
            ErrorMessage = null;
        }
        
        public void MarkAsNotRunning()
        {
            IsRunning = false;
        }
    }
}
