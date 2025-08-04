using Microsoft.AspNetCore.SignalR;
using OllamaFarmer.Server.Services;
using OllamaSharp.Models.Chat;
using System.Text.Json;

namespace OllamaFarmer.Server.Services.SignalR
{
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
}
