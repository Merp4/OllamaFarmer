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

    /// <summary>
    /// Domain model for chat images - used by business logic layer
    /// </summary>
    public class AppChatImage
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string? Url { get; set; }
        public string? Path { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? MediaType { get; set; }

        /// <summary>
        /// Convert from database entity to domain model
        /// </summary>
        public static AppChatImage FromEntity(AppImageEntity entity)
        {
            return new AppChatImage
            {
                Id = entity.Id,
                Url = entity.Url,
                Path = entity.Path,
                Description = entity.Description,
                CreatedAt = entity.CreatedAt,
                MediaType = entity.MediaType
            };
        }

        /// <summary>
        /// Convert to database entity
        /// </summary>
        public AppImageEntity ToEntity()
        {
            return new AppImageEntity
            {
                Id = Id,
                Url = Url,
                Path = Path,
                Description = Description,
                CreatedAt = CreatedAt,
                MediaType = MediaType
            };
        }
    }

    /// <summary>
    /// Domain model for chat options - used by business logic layer
    /// </summary>
    public class AppChatOptions
    {
        public float Temperature { get; set; } = 0.7f;
        public float TopP { get; set; } = 0.3f;
        public float FrequencyPenalty { get; set; } = 0.5f;
        public float PresencePenalty { get; set; } = 0.1f;
        public List<Guid> EnabledToolIds { get; set; } = new();
        public List<string> EnabledTools { get; set; } = new(); // List of tool names enabled for this chat
        public Dictionary<string, Guid> ToolToServerMapping { get; set; } = new(); // Maps tool names to their MCP server IDs
        public bool DisableMultipleToolCalls { get; set; }

        /// <summary>
        /// Convert from database entity to domain model
        /// </summary>
        public static AppChatOptions FromEntity(AppChatOptionsEntity entity)
        {
            return new AppChatOptions
            {
                Temperature = entity.Temperature,
                TopP = entity.TopP,
                FrequencyPenalty = entity.FrequencyPenalty,
                PresencePenalty = entity.PresencePenalty,
                EnabledToolIds = entity.EnabledToolIds,
                EnabledTools = entity.EnabledTools,
                ToolToServerMapping = entity.ToolToServerMapping,
                DisableMultipleToolCalls = entity.DisableMultipleToolCalls
            };
        }

        /// <summary>
        /// Convert to database entity
        /// </summary>
        public AppChatOptionsEntity ToEntity()
        {
            return new AppChatOptionsEntity
            {
                Temperature = Temperature,
                TopP = TopP,
                FrequencyPenalty = FrequencyPenalty,
                PresencePenalty = PresencePenalty,
                EnabledToolIds = EnabledToolIds,
                EnabledTools = EnabledTools,
                ToolToServerMapping = ToolToServerMapping,
                DisableMultipleToolCalls = DisableMultipleToolCalls
            };
        }
    }
}
