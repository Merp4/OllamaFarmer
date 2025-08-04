namespace OllamaFarmer.Server.Dto
{
    public class ProgressState
    {
        public string Status { get; internal set; }
        public long BytesTotal { get; internal set; }
        public long BytesCompleted { get; internal set; }
        public double PercentCompleted { get; internal set; }
    }

    public class ProgressNotification
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public int CompletedItems { get; set; } = 0;
        public int TotalItems { get; set; } = 0;
        public string? StatusMessage { get; set; }
    }
}
