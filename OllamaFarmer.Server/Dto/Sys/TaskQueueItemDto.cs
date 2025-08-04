namespace OllamaFarmer.Server.Dto.Sys
{
    public class TaskQueueDto
    {
        public int QueuedCount { get; set; }
    }
    public class TaskQueueItemDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public bool IsCompleted { get; set; }
        public bool IsRunning { get; set; }
        public bool IsFailed { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
