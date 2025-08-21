using Microsoft.Extensions.AI;
using OllamaFarmer.Server.Models;
using OllamaFarmer.Server.Dto;

namespace OllamaFarmer.Server.Services.Interfaces
{
    public interface IAppChatService
    {
    Task<OllamaFarmer.Server.Models.AppChat> AddMessageAsync(OllamaFarmer.Server.Models.AppChat chat, string content, ChatRole role, List<string>? imageUrls = null);
    Task<OllamaFarmer.Server.Models.AppChat> AddUserMessageAsync(OllamaFarmer.Server.Models.AppChat chat, string content, List<string>? imageUrls = null);
    Task<OllamaFarmer.Server.Models.AppChat> CreateChatAsync(Guid serverId, string? name = null, string? model = null, string? systemMessage = null);
    Task<OllamaFarmer.Server.Models.AppChat> CreateChatAsync(Guid serverId, string? name = null, string? model = null, string? systemMessage = null, OllamaFarmer.Server.Models.AppChatOptions? options = null);
        Task CreateModelFromChatAsync(Guid id, ModelRequest request);
        Task DeleteChatAsync(Guid id);
        Task DeleteMessageAsync(Guid messageId);
    Task<OllamaFarmer.Server.Models.AppChat?> GetChatAsync(Guid id);
    string GetConversationText(OllamaFarmer.Server.Models.AppChat chat);
    Task<List<OllamaFarmer.Server.Models.AppChat>> ListChatsAsync(int skip = 0, int take = 50);
    Task<List<OllamaFarmer.Server.Models.AppChat>> ListChatsAsync(Guid serverId, int skip = 0, int take = 50);
    Task SaveChatAsync(OllamaFarmer.Server.Models.AppChat chat);
    Task<OllamaFarmer.Server.Models.AppChat> SubmitChatAsync(Guid serverId, OllamaFarmer.Server.Models.AppChat chat);
    Task UpdateChatOptions(Guid id, OllamaFarmer.Server.Models.AppChatOptions options);
        Task UpdateMessageAsync(Guid messageId, ChatRole role, string message);
    Task<OllamaFarmer.Server.Models.AppChat> CloneChatAsync(Guid sourceChatId, string? newName = null);
        Task<string> GenerateResponseAsync(Guid serverId, string model, string systemMessage, string userMessage, float temperature, float topP, float frequencyPenalty, float presencePenalty);
    }
}
