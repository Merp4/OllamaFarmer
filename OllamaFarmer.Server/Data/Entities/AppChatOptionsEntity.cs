//using OllamaSharp.Models.Chat;

namespace OllamaFarmer.Server.Data.Entities
{
    public class AppChatOptionsEntity
    {
        public float Temperature { get; set; } = 0.7f;
        public float TopP { get; set; } = 0.3f;
        public float FrequencyPenalty { get; set; } = 0.5f;
        public float PresencePenalty { get; set; } = 0.1f;
        public List<Guid> EnabledToolIds { get; set; } = new(); // MCP tool IDs to enable for this chat
        public List<string> EnabledTools { get; set; } = new(); // List of tool names enabled for this chat
        public Dictionary<string, Guid> ToolToServerMapping { get; set; } = new(); // Maps tool names to their MCP server IDs
        public bool DisableMultipleToolCalls { get; set; }
    }
}
