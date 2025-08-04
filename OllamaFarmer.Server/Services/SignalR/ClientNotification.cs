namespace OllamaFarmer.Server.Services.SignalR
{
    public class ClientNotification
    {
        public string? ConnectionId { get; set; }
        public string? UserName { get; set; }
        public string? UserId { get; set; }
        public string? Message { get; set; }
        public DateTime? Timestamp { get; set; } = DateTime.UtcNow;
        public ClientNotificationType Type { get; set; } = ClientNotificationType.Default;

        public override string ToString()
        {
            return $"{Timestamp:yyyy-MM-dd HH:mm:ss} [{UserId}] {UserName}: {Message}";
        }
    }

    public enum ClientNotificationType
    {
        Default,
        Info,
        Success,
        Warning,
        Error,
    }
}
