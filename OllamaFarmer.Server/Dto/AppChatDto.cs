using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.AI;
using OllamaFarmer.Server.Data.Entities;
using OllamaFarmer.Server.Models;
using OllamaFarmer.Server.Services.Ollama;

namespace OllamaFarmer.Server.Dto
{
    public class AppChatDto
    {

        public AppChatDto(Models.AppChat chat) { 
            Id = chat.Id;
            ChatServerId = chat.ChatServerId;
            Name = chat.Name;
            Persist = true;
            CreatedAt = chat.CreatedAt;
            Messages = new List<AppMessageDto>();
            Model = chat.Model;
            Options = new Models.AppChatOptions
            {
                Temperature = chat.Options.Temperature,
                TopP = chat.Options.TopP,
                FrequencyPenalty = chat.Options.FrequencyPenalty,
                PresencePenalty = chat.Options.PresencePenalty,
                EnabledToolIds = chat.Options.EnabledToolIds ?? new List<Guid>()
            };
            ModelCapabilities = chat.ModelCapabilities;
            foreach (var message in chat.Messages)
            {
                Messages.Add(new AppMessageDto
                {
                    Index = message.Index,
                    Id = message.Id,
                    Role = message.Role,
                    Content = message.Content,
                    CreatedAt = message.CreatedAt,
                    Images = message.Images.Select(i => i.Url ?? i.Path ?? string.Empty).Where(s => !string.IsNullOrEmpty(s)).ToList(),
                    ToolIds = chat.Options.EnabledToolIds ?? new List<Guid>()
                });
            }
        }

        private List<string> GetImages(Models.AppChatMessage message)
        {
            return message.Images.Select(i => i.Url ?? i.Path ?? string.Empty).Where(s => !string.IsNullOrEmpty(s)).ToList();
        }

        public AppChatDto(Data.Entities.AppChatEntity chat)
        {
            Id = chat.Id;
            ChatServerId = chat.ChatServerId; 
            Name = chat.Name;
            Persist = chat.Persist;
            CreatedAt = chat.CreatedAt;
            Messages = new List<AppMessageDto>();
            Model = chat.Model;
            Options = Models.AppChatOptions.FromEntity(chat.Options);
            ModelCapabilities = chat.ModelCapabilities;
            foreach (var message in chat.Messages)
            {
                Messages.Add(new AppMessageDto
                {
                    Index = message.Index,
                    Id = message.Id,
                    Role = message.Role,
                    Content = message.Content,
                    CreatedAt = message.CreatedAt,
                    Images = message.Images?.Select(img => img.Url ?? string.Empty).ToList() ?? new List<string>()
                });
            }
        }

        public Guid Id { get; set; }
        public Guid ChatServerId { get; set; }
        public string? Name { get; set; }
        public bool Persist { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<AppMessageDto> Messages { get; set; }
        public string? Model { get; set; }
        public Models.AppChatOptions Options { get; set; }
        public ModelCapabilities ModelCapabilities { get; set; } = new();
    }

    public class AppMessageDto
    {
        public AppMessageDto() 
        {
        }
        public AppMessageDto(Models.AppChatMessage? appChatMessage, List<string>? tools)
        {
            if (appChatMessage == null) return;
            Index = appChatMessage.Index;
            Id = appChatMessage.Id;
            Role = appChatMessage.Role;
            Content = appChatMessage.Content;
            CreatedAt = appChatMessage.CreatedAt;
            Images = appChatMessage.Images.Select(i => i.Url ?? i.Path ?? string.Empty).Where(s => !string.IsNullOrEmpty(s)).ToList();
            ToolIds = tools?.Select(t => Guid.Parse(t)).ToList() ?? new List<Guid>();
        }
        public AppMessageDto(Models.AppChatMessage? appChatMessage, List<Guid>? tools)
        {
            if (appChatMessage == null) return;
            Index = appChatMessage.Index;
            Id = appChatMessage.Id;
            Role = appChatMessage.Role;
            Content = appChatMessage.Content;
            CreatedAt = appChatMessage.CreatedAt;
            Images = appChatMessage.Images.Select(i => i.Url ?? i.Path ?? string.Empty).Where(s => !string.IsNullOrEmpty(s)).ToList();
            ToolIds = tools ?? new List<Guid>();
        }

        public int Index { get; set; }
        public Guid Id { get; set; }
        public ChatRole? Role { get; set; }
        public string? Content { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<string> Images { get; set; } = new();
        public List<Guid> ToolIds { get; set; } = new();
    }
}
