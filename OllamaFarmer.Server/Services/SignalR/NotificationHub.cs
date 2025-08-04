using Microsoft.AspNetCore.SignalR;
using OllamaFarmer.Server.Services;
using OllamaSharp.Models.Chat;
using System.Text.Json;

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

    public class NotificationHub : Hub
    {
        private readonly ILogger<NotificationHub> _logger;
        private readonly static ConnectionMapping<string> _connections =
            new ConnectionMapping<string>();

        public NotificationHub(ILogger<NotificationHub> logger)
        {
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();

            _logger.LogInformation($"Client connected: {Context.ConnectionId}");

            string name = Context.User.Identity.Name;
            _logger.LogInformation($"Client name: {name} (ConnectionId: {Context.ConnectionId})");
            var httpContext = Context.GetHttpContext().Request.Headers;

            if (string.IsNullOrEmpty(name))
            {
                _logger.LogWarning($"Client connected without a name: {Context.ConnectionId}");
                name = Context.ConnectionId;
            }

            _connections.Add(name, Context.ConnectionId);

            await Clients.Caller.SendAsync("Notification", new ClientNotification { Message = "SignalR connected as " + name });
            await Clients.AllExcept(Context.ConnectionId).SendAsync("Notification", new ClientNotification { Message = "Another user connected" });
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await base.OnDisconnectedAsync(exception);

            string name = Context.User.Identity.Name;

            _connections.Remove(name, Context.ConnectionId);
            _logger.LogInformation($"Client disconnected: {Context.ConnectionId}");
        }

        public async Task BroadcastNotification(string type, string message)
        {
            _logger.LogInformation($"Received message: {message}");

            await Clients.All.SendAsync("Notification", new ClientNotification { Type = Enum.Parse<ClientNotificationType>(type, true), Message = message });
        }

        public async Task BroadcastRawNotification(string message)
        {
            _logger.LogInformation($"Received raw message: {JsonSerializer.Serialize(message)}");

            await Clients.All.SendAsync("Notification", message);
        }

    }


    public class ConnectionMapping<T>
    {
        private readonly Dictionary<T, HashSet<string>> _connections =
            new Dictionary<T, HashSet<string>>();

        public int Count
        {
            get
            {
                return _connections.Count;
            }
        }

        public void Add(T key, string connectionId)
        {
            if (string.IsNullOrEmpty(connectionId))
            {
                return;
            }
            lock (_connections)
            {
                HashSet<string> connections;
                if (!_connections.TryGetValue(key, out connections))
                {
                    connections = new HashSet<string>();
                    _connections.Add(key, connections);
                }

                lock (connections)
                {
                    connections.Add(connectionId);
                }
            }
        }

        public IEnumerable<string> GetConnections(T key)
        {
            HashSet<string> connections;
            if (_connections.TryGetValue(key, out connections))
            {
                return connections;
            }

            return Enumerable.Empty<string>();
        }

        public void Remove(T key, string connectionId)
        {
            if (string.IsNullOrEmpty(connectionId) || key == null)
            {
                return;
            }

            lock (_connections)
            {
                HashSet<string> connections;
                if (!_connections.TryGetValue(key, out connections))
                {
                    return;
                }

                lock (connections)
                {
                    connections.Remove(connectionId);

                    if (connections.Count == 0)
                    {
                        _connections.Remove(key);
                    }
                }
            }
        }
    }
}
