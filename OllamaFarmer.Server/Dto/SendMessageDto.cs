//using OllamaSharp.Models.Chat;
using Microsoft.Extensions.AI;

namespace OllamaFarmer.Server.Dto
{
    public class SendMessageDto
    {
        public string Message { get; set; }
        public ChatRole Role { get; set; }

        public List<string> Images { get; set; }
        public float? Temperature { get; internal set; }
        public float? TopP { get; internal set; }
        public float? FrequencyPenalty { get; internal set; }
        public float? PresencePenalty { get; internal set; }

        public List<Guid> EnabledToolIds { get; set; } = new();
        public List<Guid> EnabledToolBagIds { get; set; } = new();
        public string? ToolCallId { get; internal set; }
        public string? ToolCallName { get; internal set; }
        public Dictionary<string, object?>? ToolCallArgs { get; internal set; }
    }
}
