using OllamaFarmer.Server.Data.Entities;
using OllamaFarmer.Server.Dto;
using OllamaFarmer.Server.Models;
using OllamaSharp.Models;

namespace OllamaFarmer.Server.Services.Ollama.Interfaces
{
    public interface IOllamaModelService
    {
        Task<ChatModel?> CreateChatModelAsync(ChatModel chatModel);
        Task<bool> DeleteChatModelAsync(Guid id, bool deleteRemote);
        Task<IEnumerable<ChatModelDto>> GetAllChatModelsAsync(Guid serverId);
        Task<List<ChatModelAndDetails>> GetAllModelDetailsAsync(Guid serverId);
        Task<List<Model>> GetAvailableApiModels(Guid serverId);
        Task<ModelCapabilities?> GetModelCapabilitiesAsync(Guid serverId, string model);
        Task<ChatModelAndDetails?> GetModelDetailsAsync(Guid serverId, Guid id);
        Task<IEnumerable<RunningModel>> GetRunningApiModels(Guid serverId);
        IAsyncEnumerable<ProgressState> PullModelAsync(Guid serverId, string model);
        Task RefreshCapabilitiesAsync(Guid serverId, Guid[]? chatModelIds = null);
        Task<ChatModel?> UpdateChatModelAsync(ChatModel chatModel);
    }
}
