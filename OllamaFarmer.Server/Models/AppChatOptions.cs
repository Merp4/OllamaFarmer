using OllamaFarmer.Server.Data.Entities;

namespace OllamaFarmer.Server.Models
{
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
