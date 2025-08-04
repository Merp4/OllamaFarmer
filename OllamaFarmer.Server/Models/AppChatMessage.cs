using Microsoft.Extensions.AI;
using OllamaFarmer.Server.Data.Entities;

namespace OllamaFarmer.Server.Models
{
    /// <summary>
    /// Domain model for chat messages - used by business logic layer
    /// </summary>
    public class AppChatMessage
    {
        public int Index { get; set; }
        public Guid Id { get; set; } = Guid.NewGuid();
        public ChatRole? Role { get; set; }
        public string? Content { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public List<AppChatImage> Images { get; set; } = new();
        public List<string> Tools { get; set; } = new();
        public string? ToolCallId { get; set; }
        public string? ToolCallName { get; set; }
        public IDictionary<string, object?>? ToolCallArgs { get; set; }
        
        // Backward compatibility properties
        public ChatMessage ApiChatMessage { get; set; } = new(); // For compatibility with existing service code
        public List<string> ImageUrls { get; set; } = new(); // For compatibility with existing service code
        public bool IsCompletedFunctionCall { get; set; } = false;
        public bool IsCompletedToolResult { get; set; } = false;

        /// <summary>
        /// Convert from database entity to domain model
        /// </summary>
        public static AppChatMessage FromEntity(AppMessageEntity entity)
        {
            return new AppChatMessage
            {
                Index = entity.Index,
                Id = entity.Id,
                Role = entity.Role,
                Content = entity.Content,
                CreatedAt = entity.CreatedAt,
                Images = entity.Images.Select(AppChatImage.FromEntity).ToList(),
                Tools = entity.Tools,
                ToolCallId = entity.ToolCallId,
                ToolCallName = entity.ToolCallName,
                ToolCallArgs = entity.ToolCallArgs,
                ImageUrls = entity.ImageUrls,
                IsCompletedFunctionCall = entity.IsCompletedFunctionCall,
                IsCompletedToolResult = entity.IsCompletedToolResult
            };
        }

        /// <summary>
        /// Convert to database entity
        /// </summary>
        public AppMessageEntity ToEntity()
        {
            return new AppMessageEntity
            {
                Index = Index,
                Id = Id,
                Role = Role,
                Content = Content,
                CreatedAt = CreatedAt,
                Images = Images.Select(i => i.ToEntity()).ToList(),
                Tools = Tools,
                ToolCallId = ToolCallId,
                ToolCallName = ToolCallName,
                ToolCallArgs = ToolCallArgs,
                Timestamp = CreatedAt, // Map to backward compatibility property
                IsCompleted = IsCompletedFunctionCall || IsCompletedToolResult,
                ImageUrls = ImageUrls,
                IsCompletedFunctionCall = IsCompletedFunctionCall,
                IsCompletedToolResult = IsCompletedToolResult
            };
        }
    }
}
