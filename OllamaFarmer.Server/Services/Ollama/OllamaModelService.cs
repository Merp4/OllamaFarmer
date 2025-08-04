using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using OllamaFarmer.Server.Models;
using OllamaSharp;
using OllamaSharp.Models;
using OllamaFarmer.Server.Data;
using OllamaFarmer.Server.Data.Entities;
using Microsoft.Extensions.AI;
using OllamaFarmer.Server.Dto;
using OllamaFarmer.Server.Services.Ollama.Interfaces;
using OllamaFarmer.Server.Services.Background.Interfaces;

namespace OllamaFarmer.Server.Services.Ollama
{
    public class OllamaModelService : IOllamaModelService
    {
        private static class CacheKeys
        {
            public const string LocalModels = "LocalModels";
            public const string RunningModels = "RunningModels";
            public const string ModelSummaries = "ModelSummaries";
            public const string ModelDetails = "ModelDetails";
            public const string DbChatModels = "DbChatModels";
        }

        ILogger<OllamaModelService> _logger;

        //IChatClientService chatProvider;
        IOllamaApiClientFactory _ollamaApiClientFactory;
        IBackgroundTaskQueue _backgroundTaskQueue;

        IMemoryCache _cache;
        private readonly IDbContextFactory<AppDbContext> dbContextFactory;

        //private Task<List<Model>> LocalModels
        //    => _cache.GetCachedOrAsync(
        //        CacheKeys.LocalModels,
        //        async () => (await _ollamaClient.ListLocalModelsAsync())?.ToList() ?? new List<Model>(),
        //        10 * 60,
        //        new PostEvictionCallbackRegistration { EvictionCallback = Evicted }
        //    );

        //private Task<List<ChatModel>> DbChatModels
        //    => _cache.GetCachedOrAsync(
        //        CacheKeys.DbChatModels,
        //        async () =>
        //        {
        //            using var dbContext = await dbContextFactory.CreateDbContextAsync();
        //            return await dbContext.Set<ChatModel>().ToListAsync();
        //        },
        //        120,
        //        new PostEvictionCallbackRegistration { EvictionCallback = Evicted }
        //    );

        public OllamaModelService(IDbContextFactory<AppDbContext> dbContextFactory,
            ILogger<OllamaModelService> logger, IBackgroundTaskQueue backgroundTaskQueue,
            IOllamaApiClientFactory ollamaApiClientFactory, IMemoryCache memoryCache
            )
        {
            this.dbContextFactory = dbContextFactory;
            _logger = logger;
            _backgroundTaskQueue = backgroundTaskQueue;
            _ollamaApiClientFactory = ollamaApiClientFactory;
            _cache = memoryCache;
        }

        private async Task<OllamaApiClient> GetApiClientAsync(Guid serverId)
        {
            var server = await GetChatServer(serverId);
            if (server == null)
            {
                _logger.LogError("Chat server with ID {ServerId} not found.", serverId);
                throw new InvalidOperationException($"Chat server with ID {serverId} not found.");
            }
            var ollamaClient = _ollamaApiClientFactory.CreateClient(server.Uri, "");
            return ollamaClient;
        }
        private async Task<ChatServer?> GetChatServer(Guid serverId)
        {
            using var dbContext = dbContextFactory.CreateDbContext();
            var server = await dbContext.ChatServers.FirstOrDefaultAsync(s => s.Id == serverId);
            return server;
        }

        public async Task<List<Model>> GetAvailableApiModels(Guid serverId)
        {
            var client = await GetApiClientAsync(serverId);
            return (await client.ListLocalModelsAsync())?.ToList() ?? new List<Model>();
        }

        public async Task<ModelCapabilities?> GetModelCapabilitiesAsync(Guid serverId, string model)
        {
            if (string.IsNullOrWhiteSpace(model))
            {
                throw new ArgumentException("Model name cannot be null or empty.", nameof(model));
            }

            return await _cache.GetOrCreateAsync(
                $"{CacheKeys.ModelSummaries}:{model}",
                async (entry) =>
                {
                    entry.SetSlidingExpiration(TimeSpan.FromMinutes(5));
                    var client = await GetApiClientAsync(serverId);
                    var modelInfo = await client.ShowModelAsync(model);
                    
                    _logger.LogInformation("GetModelCapabilitiesAsync for model {Model}: ModelInfo is null = {IsNull}", 
                        model, modelInfo == null);
                    
                    if (modelInfo != null)
                    {
                        _logger.LogInformation("ModelInfo.Capabilities is null = {IsNull}, Capabilities = {Capabilities}", 
                            modelInfo.Capabilities == null, 
                            modelInfo.Capabilities != null ? string.Join(",", modelInfo.Capabilities) : "null");
                    }
                    
                    var capabilities = new ModelCapabilities(modelInfo?.Capabilities);
                    
                    // If capabilities are empty, provide default capabilities for chat models
                    if (capabilities.Strings.Length == 0)
                    {
                        _logger.LogInformation("No capabilities found for model {Model}, using default capabilities", model);
                        capabilities = new ModelCapabilities(new[] { "completion" });
                    }
                    
                    _logger.LogInformation("Created ModelCapabilities: Strings={Strings}, SupportsCompletion={SupportsCompletion}", 
                        string.Join(",", capabilities.Strings), capabilities.SupportsCompletion);
                    
                    return capabilities;
                }
            );
        }

        public async Task<IEnumerable<RunningModel>> GetRunningApiModels(Guid serverId)
        {
            var client = await GetApiClientAsync(serverId);
            return await client.ListRunningModelsAsync();
        }

        public async IAsyncEnumerable<ProgressState> PullModelAsync(Guid serverId, string model)
        {
            var client = await GetApiClientAsync(serverId);
            // Remote models available at https://ollama.com/library
            await foreach (var streamedResponse in client.PullModelAsync(new PullModelRequest { Stream = true, Model = model }))
            {
                if (streamedResponse == null)
                    yield break;
                yield return new ProgressState
                {
                    Status = streamedResponse.Status,
                    BytesCompleted = streamedResponse.Completed,
                    PercentCompleted = streamedResponse.Percent,
                    BytesTotal = streamedResponse.Total
                };
            }
            // TODO: Consider cache invalidation after model pull
            // _cache.Remove(CacheKeys.LocalModels); 
            // _cache.Remove(CacheKeys.ModelDetails);
        }

        public async Task<List<ChatModelAndDetails>> GetAllModelDetailsAsync(Guid serverId)
        {
            // Remote models available at https://ollama.com/library
            // TODO: Consider caching for performance optimization
            // if (_cache.TryGetValue(CacheKeys.ModelDetails, out List<ChatModelAndDetails>? cachedModels) && cachedModels != null && cachedModels.Count > 0)
            //    return cachedModels;

            var localModels = await GetAvailableApiModels(serverId);
            var runningModels = await GetRunningApiModels(serverId);
            
            using var dbContext = await dbContextFactory.CreateDbContextAsync();
            var dbModels = await dbContext.Models.Where(m => m.ChatServerId == serverId).ToListAsync();

            var (chatModelAndDetails, chatModelsToCreate) = await GetAndSynchroniseChatsAsync(serverId, localModels, runningModels.ToList(), dbModels);

            if (chatModelsToCreate.Count > 0)
            {
                await dbContext.Set<ChatModel>().AddRangeAsync(chatModelsToCreate);
                try
                {
                    await dbContext.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error saving new chat models to database");

                }
            }
            return chatModelAndDetails.OrderByDescending(c => c.ChatModel.CreatedAt).ToList();
        }

        private async Task<(List<ChatModelAndDetails> chatModelAndDetails, List<ChatModel> chatModelsToCreate)>
            GetAndSynchroniseChatsAsync(Guid serverId, List<Model> models, List<RunningModel> runningModels, List<ChatModel> dbModels)
        {
            _logger.LogInformation("Synchronizing chat models for server {ServerId}. API models: {ApiCount}, DB models: {DbCount}", 
                serverId, models.Count, dbModels.Count);

            // Create dictionaries for efficient lookups
            var apiModelDict = models.ToDictionary(m => m.Name, m => m);
            var dbModelDict = dbModels.ToDictionary(m => m.Name ?? "", m => m);
            var runningModelDict = runningModels?.ToDictionary(m => m.Name, m => m) ?? new Dictionary<string, RunningModel>();

            List<ChatModel> modelsToCreate = new List<ChatModel>();

            // Step 1: Ensure all API models exist in database
            // If a model exists in Ollama API but not in DB, create it
            foreach (var apiModel in models)
            {
                if (!dbModelDict.ContainsKey(apiModel.Name))
                {
                    _logger.LogInformation("Creating new ChatModel for API model: {ModelName}", apiModel.Name);
                    
                    var newModel = new ChatModel
                    {
                        ChatServerId = serverId,
                        Name = apiModel.Name,
                        Model = apiModel.Name,
                        ParameterSize = apiModel.Details?.ParameterSize ?? "unknown",
                        Size = apiModel.Size,
                        Description = apiModel.Details?.QuantizationLevel ?? "N/A",
                        QuantizationLevel = apiModel.Details?.QuantizationLevel ?? "unknown",
                        Family = apiModel.Details?.Family ?? "unknown",
                        Url = null,
                        ImageUrl = null,
                        IsLocal = true,  // Model exists in API, so it's local
                        IsRemote = false,
                        IsChatModel = true,  // Assume all models can chat
                        IsLLMModel = true,   // Assume all models are LLMs
                        CreatedAt = DateTime.UtcNow,
                        Vram = 0,
                        Capabilities = (await GetModelCapabilitiesAsync(serverId, apiModel.Name))?.Strings.ToList() ?? ["completion"]
                    };
                    modelsToCreate.Add(newModel);
                    dbModelDict[apiModel.Name] = newModel; // Add to dict for later processing
                }
            }

            // Step 2: Update existing database models based on API presence
            foreach (var dbModel in dbModels)
            {
                if (apiModelDict.ContainsKey(dbModel.Name ?? ""))
                {
                    // Model exists in both API and DB - mark as local
                    if (!dbModel.IsLocal)
                    {
                        _logger.LogInformation("Marking model {ModelName} as local (found in API)", dbModel.Name);
                        dbModel.IsLocal = true;
                        dbModel.IsRemote = false;
                    }
                }
                else
                {
                    // Model exists in DB but not in API - mark as remote (not pulled)
                    if (dbModel.IsLocal)
                    {
                        _logger.LogInformation("Marking model {ModelName} as remote (not found in API)", dbModel.Name);
                        dbModel.IsLocal = false;
                        dbModel.IsRemote = true;
                    }
                }
            }

            // Step 3: Create ChatModelAndDetails objects for all models (existing + new)
            var allDbModels = dbModels.Union(modelsToCreate);
            var modelDetails = allDbModels.Select(dbModel =>
            {
                // Find corresponding API model
                var apiModel = apiModelDict.GetValueOrDefault(dbModel.Name ?? "");
                
                // Find if model is currently running
                var runningModel = runningModelDict.GetValueOrDefault(dbModel.Model ?? "");
                
                var chatModelDto = new ChatModelDto(dbModel);
                
                // Update running status and VRAM if model is running
                if (runningModel != null)
                {
                    chatModelDto.IsRunning = true;
                    chatModelDto.Vram = runningModel.SizeVram;
                    _logger.LogDebug("Model {ModelName} is currently running with VRAM: {Vram}", dbModel.Name, runningModel.SizeVram);
                }
                else
                {
                    chatModelDto.IsRunning = false;
                    chatModelDto.Vram = 0;
                }

                return new ChatModelAndDetails
                {
                    ChatModel = chatModelDto,
                    ApiModel = apiModel != null ? new ModelDto(apiModel) : null
                };
            }).ToList();

            _logger.LogInformation("Synchronization complete. Models to create: {CreateCount}, Total models: {TotalCount}", 
                modelsToCreate.Count, modelDetails.Count);

            return (modelDetails, modelsToCreate);
        }


        public async Task<ChatModelAndDetails?> GetModelDetailsAsync(Guid serverId, Guid id)
        {
            var detailsCache = await GetAllModelDetailsAsync(serverId);

            return detailsCache?.SingleOrDefault(x => x.ChatModel.Id == id);
        }

        public async Task<IEnumerable<ChatModelDto>> GetAllChatModelsAsync()
        {
            using var dbContext = await dbContextFactory.CreateDbContextAsync();
            return await dbContext.Set<ChatModel>().Select(m => new ChatModelDto(m)).ToListAsync();
        }

        public async Task RefreshCapabilitiesAsync(Guid serverId, Guid[]? chatModelIds = null)
        {
            await _backgroundTaskQueue.EnqueueAsync(async token =>
            {
                try
                {
                    await RefreshCapabilitiesTaskAsync(serverId, chatModelIds);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error refreshing capabilities in background task");
                }
            });
        }

        private async Task RefreshCapabilitiesTaskAsync(Guid serverId, Guid[]? chatModelIds = null)
        {

            using var dbContext = await dbContextFactory.CreateDbContextAsync();

            // If no specific IDs are provided, refresh all models
            if (chatModelIds == null || chatModelIds.Length == 0)
            {
                chatModelIds = await dbContext.Set<ChatModel>().Select(m => m.Id).ToArrayAsync();
            }

            foreach (var id in chatModelIds)
            {
                var chatModel = await dbContext.Set<ChatModel>()
                    .FirstOrDefaultAsync(m => m.Id == id && m.ChatServerId == serverId);
                if (chatModel == null)
                {
                    _logger.LogWarning("ChatModel with id {Id} and serverId {ServerId} not found", id, serverId);
                    continue;
                }
                try
                {
                    _logger.LogInformation("Refreshing capabilities for model {ModelName} (ID: {Id})", chatModel.Model, id);
                    
                    if (string.IsNullOrEmpty(chatModel.Model))
                    {
                        _logger.LogWarning("ChatModel {Id} has no model name, skipping capability refresh", id);
                        continue;
                    }
                    
                    var capabilities = await GetModelCapabilitiesAsync(serverId, chatModel.Model);
                    if (capabilities != null)
                    {
                        _logger.LogInformation("Retrieved capabilities for model {ModelName}: Strings={Strings}, SupportsCompletion={SupportsCompletion}, SupportsTools={SupportsTools}, SupportsVision={SupportsVision}", 
                            chatModel.Model, 
                            string.Join(",", capabilities.Strings), 
                            capabilities.SupportsCompletion, 
                            capabilities.SupportsTools, 
                            capabilities.SupportsVision);
                        
                        // Preserve custom "thinking" capability based on EnableThinking setting
                        var newCapabilities = capabilities.Strings.ToList();
                        var hasThinkingCapability = chatModel.Capabilities?.Contains("thinking", StringComparer.OrdinalIgnoreCase) ?? false;
                        
                        // Add thinking capability if EnableThinking is true and it's not already present
                        if (chatModel.EnableThinking && !newCapabilities.Contains("thinking", StringComparer.OrdinalIgnoreCase))
                        {
                            newCapabilities.Add("thinking");
                        }
                        // Remove thinking capability if EnableThinking is false
                        else if (!chatModel.EnableThinking)
                        {
                            newCapabilities.RemoveAll(c => c.Equals("thinking", StringComparison.OrdinalIgnoreCase));
                        }
                        
                        chatModel.Capabilities = newCapabilities;
                        chatModel.IsLLMModel = capabilities.SupportsCompletion;
                        chatModel.IsChatModel = capabilities.SupportsCompletion;
                        
                        _logger.LogInformation("Updated ChatModel capabilities: Count={Count}, Items={Items}", 
                            chatModel.Capabilities.Count, 
                            string.Join(",", chatModel.Capabilities));
                        
                        dbContext.Update(chatModel);
                        _logger.LogInformation("Successfully refreshed capabilities for model {ModelName}", chatModel.Model);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to get capabilities for model {ModelName}, capabilities were null", chatModel.Model);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error refreshing capabilities for model {modelName}", chatModel.Model);
                }
            }
            try
            {
                await dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving updated chat models to database");
            }
        }

        public async Task<bool> DeleteChatModelAsync(Guid id, bool deleteRemote)
        {
            try
            {
                using var dbContext = await dbContextFactory.CreateDbContextAsync();
                var chatModel = await dbContext.Models.SingleOrDefaultAsync(Models => Models.Id == id);
                if (chatModel == null)
                    return false;

                dbContext.Set<ChatModel>().Remove(chatModel);
                var oClient = await GetApiClientAsync(chatModel.ChatServerId);
                if (chatModel.Model != null && deleteRemote)
                {
                    _logger.LogInformation("Deleting remote model {ModelName} from server {ServerId}", chatModel.Model, chatModel.ChatServerId);
                    await oClient.DeleteModelAsync(chatModel.Model);
                }

                await dbContext.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting chat model with ID {id}", id);
                return false;
            }
        }

        public async Task<ChatModel?> CreateChatModelAsync(ChatModel chatModel)
        {
            // Check if the model exists on the chat server
            if (string.IsNullOrWhiteSpace(chatModel.Model))
            {
                _logger.LogError("Chat model creation failed: Model name cannot be null or empty.");
                return null;
            }

            var oClient = await GetApiClientAsync(chatModel.ChatServerId);
            
            var existingModel = await oClient.ShowModelAsync(chatModel.Model);
            if (existingModel == null)
            {
                chatModel.IsLocal = false; // Model is not local
            }
            else
            {

                // get capabilities
                var capabilities = await GetModelCapabilitiesAsync(chatModel.ChatServerId, chatModel.Model);
                if (capabilities != null)
                {
                    var modelCapabilities = capabilities.Strings.ToList();
                    
                    // Add thinking capability if EnableThinking is true
                    if (chatModel.EnableThinking && !modelCapabilities.Contains("thinking", StringComparer.OrdinalIgnoreCase))
                    {
                        modelCapabilities.Add("thinking");
                    }
                    
                    chatModel.Capabilities = modelCapabilities;
                    chatModel.IsLLMModel = capabilities.SupportsCompletion;
                    chatModel.IsChatModel = capabilities.SupportsCompletion;
                }
                else
                {
                    _logger.LogWarning("Capabilities for model {model} could not be retrieved.", chatModel.Model);
                    
                    // Even if we can't get Ollama capabilities, set thinking if enabled
                    if (chatModel.EnableThinking)
                    {
                        chatModel.Capabilities = new List<string> { "thinking" };
                    }
                }
            }

            using var dbContext = await dbContextFactory.CreateDbContextAsync();
            dbContext.Set<ChatModel>().Add(chatModel);
            try
            {
                await dbContext.SaveChangesAsync();
                return chatModel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating chat model");
                return null;
            }
        }

        public async Task<ChatModel?> UpdateChatModelAsync(ChatModel chatModel)
        {
            using var dbContext = await dbContextFactory.CreateDbContextAsync();
            var existingModel = await dbContext.Set<ChatModel>().FindAsync(chatModel.Id);
            if (existingModel == null)
                return null;
            existingModel.Name = chatModel.Name;
            existingModel.Model = chatModel.Model;
            existingModel.ParameterSize = chatModel.ParameterSize;
            existingModel.Size = chatModel.Size;
            existingModel.Description = chatModel.Description;
            existingModel.QuantizationLevel = chatModel.QuantizationLevel;
            existingModel.Url = chatModel.Url;
            existingModel.ImageUrl = chatModel.ImageUrl;
            existingModel.Family = chatModel.Family;
            existingModel.IsLocal = chatModel.IsLocal;
            existingModel.IsRemote = chatModel.IsRemote;
            existingModel.IsChatModel = chatModel.IsChatModel;
            existingModel.IsLLMModel = chatModel.IsLLMModel;
            existingModel.Capabilities = chatModel.Capabilities;
            try
            {
                await dbContext.SaveChangesAsync();
                return existingModel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating chat model with ID {id}", chatModel.Id);
                return null;
            }
        }

    }
}
