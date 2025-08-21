using System;
using System.Collections.Generic;

namespace OllamaFarmer.Data.Entities
{
    public class AppMessageEntity
    {
        public int Index { get; set; }
        public Guid Id { get; set; } = Guid.NewGuid();
        // Store role as a raw string; provider layer maps to typed role
        public string? Role { get; set; }
        public string? Content { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public List<AppImageEntity> Images { get; set; } = new();
        public List<string> Tools { get; set; } = new();
        public string? ToolCallId { get; set; }
        public string? ToolCallName { get; set; }
        public IDictionary<string, object?>? ToolCallArgs { get; set; }

        // Foreign key and navigation property
        public Guid? AppChatEntityId { get; set; }
        public AppChatEntity? AppChat { get; set; }

        // Backward compatibility properties
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public bool IsCompleted { get; set; }
        public Guid OllamaApiChatMessage { get; set; }
        public Guid ApiChatMessage { get; set; }
        public List<string> ImageUrls { get; set; } = new();
        public bool IsCompletedFunctionCall { get; set; }
        public bool IsCompletedToolResult { get; set; }
    }
}
