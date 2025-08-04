using Microsoft.Extensions.AI;
using OllamaFarmer.Server.Models;
using OllamaFarmer.Server.Services.Ollama;

namespace OllamaFarmer.Server.Data.Entities
{
    public class AppChatEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ChatServerId { get; set; } = Guid.Empty;
        public string? Name { get; set; }
        public bool Persist { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public List<AppMessageEntity> Messages { get; set; } = new();
        public string? Model { get; set; }
        public AppChatOptionsEntity Options { get; set; } = new();
        public ModelCapabilities ModelCapabilities { get; set; } = new ();
        
        public AppChatEntity() { }
        public AppChatEntity(Guid id) { Id = id; }
    }
}
