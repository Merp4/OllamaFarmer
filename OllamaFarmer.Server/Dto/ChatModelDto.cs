using ModelContextProtocol.Protocol;
using OllamaFarmer.Server.Data.Entities;
using OllamaFarmer.Server.Services.Ollama;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace OllamaFarmer.Server.Dto
{
    public class ChatModelDto
    {
        public ChatModelDto() 
        {
            Tags = new List<ChatModelTagDto>();
            QuantizationLevel = string.Empty;
            Family = string.Empty;
        }

        public ChatModelDto(ChatModel m)
        {
            Id = m.Id;
            Name = m.Name;
            Model = m.Model;
            ParameterSize = m.ParameterSize;
            Description = m.Description;
            Url = m.Url;
            ImageUrl = m.ImageUrl;
            Type = m.Type;
            IsLocal = m.IsLocal;
            IsRemote = m.IsRemote;
            IsChatModel = m.IsChatModel;
            IsLLMModel = m.IsLLMModel;
            IsRunning = m.IsRunning;
            Tags = new List<ChatModelTagDto>(m.TagsLinks.Select(t => new ChatModelTagDto { Id = t.Tag.Id, Name = t.Tag.Name, Value = t.Tag.Value }));
            QuantizationLevel = m.QuantizationLevel;
            Family = m.Family;
            Size = m.Size;
            CreatedAt = m.CreatedAt;
            Vram = m.Vram;
            Capabilities = new ModelCapabilities(m.Capabilities);
            EnableThinking = m.EnableThinking;
        }

        public Guid Id { get; set; }
        public string? Name { get; set; }
        public string? Model { get; set; }
        public string? ParameterSize { get; set; }
        public string? Description { get; set; }
        public string? Url { get; set; }
        public string? ImageUrl { get; set; }
        public string? Type { get; set; }
        public bool IsLocal { get; set; }
        public bool IsRemote { get; set; }
        public bool IsChatModel { get; set; }
        public bool IsLLMModel { get; set; }
        public bool? IsRunning { get; set; }
        public List<ChatModelTagDto> Tags { get; set; }
        public string QuantizationLevel { get; set; }
        public string Family { get; set; }
        public long Size { get; set; }
        public DateTime CreatedAt { get; set; }
        public long Vram { get; set; }
        public ModelCapabilities Capabilities { get; set; } = new ModelCapabilities();
        public bool EnableThinking { get; set; } = false;
    }

    public class ChatModelTagDto
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public string? Value { get; set; }
    }
}
