using Microsoft.Extensions.AI;
using OllamaFarmer.Server.Models;
using OllamaFarmer.Server.Dto;

namespace OllamaFarmer.Server.Services.Interfaces
{
    public interface IAppChatService
    {
        Task<AppChat> AddMessageAsync(AppChat chat, string content, ChatRole role, List<string>? imageUrls = null);
        Task<AppChat> AddUserMessageAsync(AppChat chat, string content, List<string>? imageUrls = null);
        Task<AppChat> CreateChatAsync(Guid serverId, string? name = null, string? model = null, string? systemMessage = null);
        Task<AppChat> CreateChatAsync(Guid serverId, string? name = null, string? model = null, string? systemMessage = null, AppChatOptions? options = null);
        Task CreateModelFromChatAsync(Guid id, ModelRequest request);
        Task DeleteChatAsync(Guid id);
        Task DeleteMessageAsync(Guid messageId);
        Task<AppChat?> GetChatAsync(Guid id);
        string GetConversationText(AppChat chat);
        Task<List<AppChat>> ListChatsAsync(int skip = 0, int take = 50);
        Task<List<AppChat>> ListChatsAsync(Guid serverId, int skip = 0, int take = 50);
        Task SaveChatAsync(AppChat chat);
        Task<AppChat> SubmitChatAsync(Guid serverId, AppChat chat);
        Task UpdateChatOptions(Guid id, AppChatOptions options);
        Task UpdateMessageAsync(Guid messageId, ChatRole role, string message);
        Task<AppChat> CloneChatAsync(Guid sourceChatId, string? newName = null);
        Task<string> GenerateResponseAsync(Guid serverId, string model, string systemMessage, string userMessage, float temperature, float topP, float frequencyPenalty, float presencePenalty);
    }
}
