using Microsoft.Extensions.AI;
using OllamaFarmer.Server.Data.Entities;
using OllamaFarmer.Server.Services.Ollama;

namespace OllamaFarmer.Server.Models
{
    /// <summary>
    /// Domain model for chat functionality - used by business logic layer
    /// </summary>
    public class AppChat
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ChatServerId { get; set; } = Guid.Empty; // The server this chat belongs to, for multi-server support
        public string? Name { get; set; }
        public bool Persist { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public List<AppChatMessage> Messages { get; set; } = new();
        public string? Model { get; set; }
        public AppChatOptions Options { get; set; } = new();
        public ModelCapabilities ModelCapabilities { get; set; } = new();

        public AppChat() { }
        public AppChat(Guid id) { Id = id; }

        /// <summary>
        /// Convert from database entity to domain model
        /// </summary>
        public static AppChat FromEntity(AppChatEntity entity)
        {
            return new AppChat
            {
                Id = entity.Id,
                ChatServerId = entity.ChatServerId,
                Name = entity.Name,
                Persist = entity.Persist,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt,
                Messages = entity.Messages.Select(AppChatMessage.FromEntity).ToList(),
                Model = entity.Model,
                Options = AppChatOptions.FromEntity(entity.Options),
                ModelCapabilities = entity.ModelCapabilities
            };
        }

        /// <summary>
        /// Convert to database entity
        /// </summary>
        public AppChatEntity ToEntity()
        {
            return new AppChatEntity
            {
                Id = Id,
                ChatServerId = ChatServerId,
                Name = Name,
                Persist = Persist,
                CreatedAt = CreatedAt,
                UpdatedAt = UpdatedAt,
                Messages = Messages.Select(m => m.ToEntity()).ToList(),
                Model = Model,
                Options = Options.ToEntity(),
                ModelCapabilities = ModelCapabilities
            };
        }
    }
}
