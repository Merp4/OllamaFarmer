using Microsoft.Extensions.AI;

namespace OllamaFarmer.Server.Data.Entities
{
    public class AppMessageEntity
    {
        public int Index { get; set; }
        public Guid Id { get; set; } = Guid.NewGuid();
        public ChatRole? Role { get; set; }
        public string? Content { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public List<AppImageEntity> Images { get; set; } = new();
        public List<string> Tools { get; set; } = new();
        public string? ToolCallId { get; set; }
        public string? ToolCallName { get; set; }
        public IDictionary<string, object?>? ToolCallArgs { get; set; }
        
        // Backward compatibility properties
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public bool IsCompleted { get; set; } = false;
        public List<string> ImageUrls { get; set; } = new();
        public bool IsCompletedFunctionCall { get; set; } = false;
        public bool IsCompletedToolResult { get; set; } = false;
    }
}
