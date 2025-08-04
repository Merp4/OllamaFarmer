using System;
using System.Collections.Generic;
using System.Linq;

namespace OllamaFarmer.Server.Data.Entities
{
    public class ChatModel
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ChatServerId { get; set; }


        public string? Name { get; set; }
        public string? Model { get; set; }
        public string? ParameterSize { get; set; }
        public string? Description { get; set; }
        public string? Url { get; set; }
        public string? ImageUrl { get; set; }
        public string? Type { get; set; }
        public bool IsLocal { get; set; } = true;
        public bool IsRemote { get; set; } = false;
        public bool IsChatModel { get; set; } = false;
        public bool IsLLMModel { get; set; } = false;
        public bool? IsRunning { get; set; } = false;
        public List<ChatModelTagLink> TagsLinks { get; set; } = new();
        public string QuantizationLevel { get; set; }
        public string Family { get; set; }
        public long Size { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public long Vram { get; set; }
        public List<string> Capabilities { get; set; }
        public bool EnableThinking { get; set; } = false;
    }

    public class ChatModelTagLink
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public ChatModelTag Tag { get; set; }
        public ChatModel Model { get; set; }
    }

    public class ChatModelTag
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string? Name { get; set; }
        public string? Value { get; set; }
        public List<ChatModelTagLink> TagsLinks { get; set; } = new();

        public IEnumerable<string> GetValues()
        {
            if (string.IsNullOrEmpty(Value))
                return Enumerable.Empty<string>();
            return Value.Split(',').Select(v => v.Trim()).Where(v => !string.IsNullOrEmpty(v));
        }
    }
}
