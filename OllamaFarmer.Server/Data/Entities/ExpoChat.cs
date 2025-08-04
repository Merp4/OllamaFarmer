using Microsoft.Extensions.AI;
using OllamaFarmer.Server.Models;
using OllamaFarmer.Server.Services.Ollama;
using System;
using System.Collections.Generic;
//using OllamaSharp.Models.Chat;

namespace OllamaFarmer.Server.Data.Entities
{
    public class AppChat
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string? Name { get; set; }
        public bool Persist { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public List<AppMessage> Messages { get; set; } = new();
        public string? Model { get; set; }
        public AppChatOptions Options { get; set; } = new();

        public ModelCapabilities ModelCapabilities { get; set; } = new ();
        public AppChat() { }
        public AppChat(Guid id) { Id = id; }
    }

    public class AppMessage
    {
        public int Index { get; set; }
        public Guid Id { get; set; } = Guid.NewGuid();
        public ChatRole? Role { get; set; }
        public string? Content { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public List<AppImage> Images { get; set; } = new();
        public List<string> Tools { get; set; } = new();
        public string? ToolCallId { get; set; }
        public string? ToolCallName { get; set; }
        public IDictionary<string, object?>? ToolCallArgs { get; set; }
    }

    //public class AppImageLink
    //{
    //    public AppMessage Message { get; set; }
    //    public AppImage Image { get; set; }
    //}

    public class AppImage
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string? Url { get; set; }
        public string? Path { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? MediaType { get; internal set; }
        //public List<AppImageLink> Messages { get; set; } = new();
    }

    public class AppChatOptions
    {
        public float Temperature { get; set; } = 0.7f;
        public float TopP { get; set; } = 0.3f;
        public float FrequencyPenalty { get; set; } = 0.5f;
        public float PresencePenalty { get; set; } = 0.1f;
        public List<Guid> EnabledToolIds { get; set; } = new(); // MCP tool IDs to enable for this chat
    }
}
