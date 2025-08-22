using Microsoft.AspNetCore.SignalR;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace OllamaFarmer.SignalR
{
    public class NotificationHub : Hub
    {
        private readonly ILogger<NotificationHub> _logger;
        private readonly static ConnectionMapping<string> _connections = new();

        public NotificationHub(ILogger<NotificationHub> logger)
        {
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();

            _logger.LogInformation($"Client connected: {Context.ConnectionId}");

            string? name = Context.User?.Identity?.Name;
            _logger.LogInformation($"Client name: {name} (ConnectionId: {Context.ConnectionId})");

            if (string.IsNullOrEmpty(name))
            {
                _logger.LogWarning($"Client connected without a name: {Context.ConnectionId}");
                name = Context.ConnectionId;
            }

            _connections.Add(name!, Context.ConnectionId);

            await Clients.Caller.SendAsync("Notification", new ClientNotification { Message = "SignalR connected as " + name });
            await Clients.AllExcept(Context.ConnectionId).SendAsync("Notification", new ClientNotification { Message = "Another user connected" });
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await base.OnDisconnectedAsync(exception);

            string? name = Context.User?.Identity?.Name;

            if (!string.IsNullOrEmpty(name))
                _connections.Remove(name!, Context.ConnectionId);

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

        // Group management for per-chat notifications
        public async Task JoinChatGroup(string chatId)
        {
            if (string.IsNullOrWhiteSpace(chatId))
            {
                _logger.LogWarning("JoinChatGroup called with empty chatId by {ConnectionId}", Context.ConnectionId);
                return;
            }
            await Groups.AddToGroupAsync(Context.ConnectionId, chatId);
            _logger.LogInformation("{ConnectionId} joined chat group {ChatId}", Context.ConnectionId, chatId);
        }

        public async Task LeaveChatGroup(string chatId)
        {
            if (string.IsNullOrWhiteSpace(chatId))
            {
                _logger.LogWarning("LeaveChatGroup called with empty chatId by {ConnectionId}", Context.ConnectionId);
                return;
            }
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, chatId);
            _logger.LogInformation("{ConnectionId} left chat group {ChatId}", Context.ConnectionId, chatId);
        }
    }
}
