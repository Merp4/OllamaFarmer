using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using OllamaFarmer.Server.Data;
using OllamaFarmer.Server.Models;
using OllamaFarmer.Server.Services.Interfaces;
using OllamaFarmer.Server.Dto;
using OllamaFarmer.Server.Services.Mcp;
using OllamaFarmer.Server.Services.Ollama;
using OllamaFarmer.Server.Services.Ollama.Interfaces;
using OllamaSharp.ModelContextProtocol.Server;
using System.Text.Json;

namespace OllamaFarmer.Server.Services
{

    public class AppChatServiceOptions
    {
        public string DefaultModel { get; set; } = "llama3.2";
        public string DefaultSystemMessage { get; set; } = "You are a helpful assistant.";
        
        /// <summary>
        /// When true, completed function calls and their results are excluded from future submissions.
        /// This prevents replaying already executed tool calls. Default: true
        /// </summary>
        public bool ExcludeCompletedFunctionCalls { get; set; } = true;
    }

    /// <summary>
    /// This class aims to provide a managed chat service for handling chat operations.
    /// Replacing the previous IChatClientService, it will manage chat creation, message handling, persistence/state management, and other chat-related functionalities.
    /// This will use the non streaming endpoints for simplicity.
    /// </summary>
    public class AppChatService : IAppChatService
    {
        private readonly IOllamaApiClientFactory _clientFactory;
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly ILogger<AppChatService> _logger;
        private readonly IBinaryRepository _binaryRepository;
        private readonly IMcpToolService _mcpService;
        private readonly IOllamaModelService _ollamaModelService;
        private readonly AppChatServiceOptions _options;
        private const string FunctionCallContent_AdditionalProperties_Key_McpServerId = "FunctionCallContent_AdditionalProperties_Key_McpServerId";

        public AppChatService(
            IOllamaApiClientFactory clientFactory,
            IDbContextFactory<AppDbContext> dbContextFactory,
            ILogger<AppChatService> logger,
            IBinaryRepository binaryRepository,
            IMcpToolService mcpService,
            IOllamaModelService ollamaModelService,
            AppChatServiceOptions? options)
        {
            _clientFactory = clientFactory;
            _dbContextFactory = dbContextFactory;
            _logger = logger;
            _binaryRepository = binaryRepository;
            _mcpService = mcpService;
            _ollamaModelService = ollamaModelService;
            _options = options ?? new AppChatServiceOptions();
        }

        /// <summary>
        /// Creates a new managed chat
        /// </summary>
        public async Task<AppChat> CreateChatAsync(Guid serverId, string? name = null, string? model = null, string? systemMessage = null)
        {
            return await CreateChatAsync(serverId, name, model, systemMessage, new());
        }

        public async Task<AppChat> CreateChatAsync(Guid serverId, string? name = null, string? model = null, string? systemMessage = null, AppChatOptions? options = null)
        {
            options ??= new();

            var chat = new AppChat
            {
                ChatServerId = serverId, // Set the chat server ID
                Name = name ?? (model ?? _options.DefaultModel) + " Chat",
                Model = model ?? _options.DefaultModel,
                Options = options,
                ModelCapabilities = await _ollamaModelService.GetModelCapabilitiesAsync(serverId, model ?? _options.DefaultModel) ?? new(),
            };
            // Add system message if provided
            if (!string.IsNullOrEmpty(systemMessage ?? _options.DefaultSystemMessage))
            {
                var systemChatMessage = new ChatMessage(ChatRole.System, systemMessage ?? _options.DefaultSystemMessage);
                await AddMessageToChatAsync(chat, systemChatMessage, []);
            }
            await SaveChatAsync(chat);
            return chat;
        }

        /// <summary>
        /// Loads a managed chat from persistence
        /// </summary>
        public async Task<AppChat?> GetChatAsync(Guid id)
        {
            using var db = _dbContextFactory.CreateDbContext();
            var entity = await db.AppChats
                //.ThenInclude(o => o.)
                .Include(c => c.Messages)
                .FirstOrDefaultAsync(c => c.Id == id);
            return entity != null ? AppChat.FromEntity(entity) : null;
        }

        /// <summary>
        /// Saves a managed chat to persistence
        /// </summary>
        public async Task SaveChatAsync(AppChat chat)
        {
            using var db = _dbContextFactory.CreateDbContext();
            chat.UpdatedAt = DateTime.UtcNow;

            var existing = await db.AppChats.Include(c => c.Messages).FirstOrDefaultAsync(c => c.Id == chat.Id);
            if (existing != null)
            {
                existing.Name = chat.Name;
                existing.Model = chat.Model;
                existing.Options = chat.Options.ToEntity();
                existing.ModelCapabilities = chat.ModelCapabilities;
                existing.UpdatedAt = chat.UpdatedAt;
                // Update existing messages
                // Remove messages that are no longer in the chat
                existing.Messages.RemoveAll(m => !chat.Messages.Any(cm => cm.Id == m.Id));
                existing.Messages.AddRange(chat.Messages.Where(m => !existing.Messages.Any(em => em.Id == m.Id)).Select(m => m.ToEntity()));
            }
            else
            {
                db.AppChats.Add(chat.ToEntity());
            }

            await db.SaveChangesAsync();
        }

        /// <summary>
        /// Adds a user message to the chat
        /// </summary>
        public async Task<AppChat> AddUserMessageAsync(AppChat chat, string content, List<string>? imageUrls = null)
        {
            var chatMessage = new ChatMessage(ChatRole.User, content);

            // Add image content if provided
            if (imageUrls != null && imageUrls.Any())
            {
                foreach (var imageUrl in imageUrls)
                {
                    try
                    {
                        var imageData = await _binaryRepository.GetFileBytesAsync(imageUrl);
                        if (imageData != null)
                        {
                            var meta = await _binaryRepository.GetMetadataAsync(imageUrl);
                            var dataContent = new DataContent(imageData, meta?.MimeType ?? "application/octet-stream");
                            chatMessage.Contents.Add(dataContent);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to load image {ImageUrl}", imageUrl);
                    }
                }
            }

            var msg = await AddMessageToChatAsync(chat, chatMessage, imageUrls);
            await PersistNewMessageToChat(chat, msg);
            return chat;
        }

        /// <summary>
        /// Adds a message to the chat
        /// </summary>
        public async Task<AppChat> AddMessageAsync(AppChat chat, string content, ChatRole role, List<string>? imageUrls = null)
        {
            var chatMessage = new ChatMessage(role, content);

            // Add image content if provided
            if (imageUrls != null && imageUrls.Any())
            {
                foreach (var imageUrl in imageUrls)
                {
                    try
                    {
                        var imageData = await _binaryRepository.GetFileBytesAsync(imageUrl);
                        if (imageData != null)
                        {
                            var meta = await _binaryRepository.GetMetadataAsync(imageUrl);
                            var dataContent = new DataContent(imageData, meta?.MimeType ?? "application/octet-stream");
                            chatMessage.Contents.Add(dataContent);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to load image {ImageUrl}", imageUrl);
                    }
                }
            }

            var msg = await AddMessageToChatAsync(chat, chatMessage, imageUrls);
            await PersistNewMessageToChat(chat, msg);
            return chat;
        }

        /// <summary>
        /// Creates a mapping from tool names to their MCP server IDs
        /// </summary>
        private async Task<Dictionary<string, Guid>> CreateToolToServerMapping(Guid[] enabledToolIds)
        {
            var mapping = new Dictionary<string, Guid>();
            
            using var db = _dbContextFactory.CreateDbContext();
            var tools = await db.McpTools
                .Where(t => enabledToolIds.Contains(t.Id))
                .ToListAsync();
            
            foreach (var tool in tools)
            {
                mapping[tool.Name] = tool.McpServerId;
            }
            
            return mapping;
        }

        /// <summary>
        /// Submits a chat and gets AI response - returns the assistant's response message
        /// </summary>
        public async Task<AppChat> SubmitChatAsync(Guid serverId, AppChat chat)
        {
            chat.Model = chat.Model ?? _options.DefaultModel;

            var modelCapabilities = await _ollamaModelService.GetModelCapabilitiesAsync(serverId, chat.Model);
            //var mcp = await _mcpService.GetMcpClientAsync(serverId);
            var aiChat = GetChatClientAsync(serverId, chat.Model);
            // MCP McpClient Tool AIFunction

            _logger.LogInformation("Requested tools: {ToolIds}", string.Join(", ", chat.Options.EnabledToolIds));

            var availableTools = await _mcpService.GetClientToolsAsync(chat.Options.EnabledToolIds.ToArray());
            _logger.LogInformation("{Model} available tools: {Tools}", chat.Model, string.Join(", ", availableTools.Select(t => t.Name)));

            // Create a mapping from tool name to MCP server ID for later use during tool execution
            var toolToServerMapping = await CreateToolToServerMapping(chat.Options.EnabledToolIds.ToArray());
            chat.Options.ToolToServerMapping = toolToServerMapping;

            var enabledTools = DetermineEnabledTools(chat, availableTools);
            _logger.LogInformation("{Model} enabled tools: {Tools}", chat.Model, string.Join(", ", enabledTools.Select(t => t.Name)));

            var chatOptions = CreateChatOptions(chat, modelCapabilities ?? new ModelCapabilities(), enabledTools);
            //var messages = GetChatMessageStream(chat).ToList();

            try
            {
                bool pendingToolCall = false;

                do
                {
                    var response = await aiChat.GetResponseAsync(GetChatMessageStream(chat).ToList(), chatOptions);

                    if (response != null)
                    {
                        await ProcessChatResponse(chat, response);
                        await SaveChatAsync(chat);
                        _logger.LogInformation("Chat submitted successfully for model {Model}. Response contains {MessageCount} messages.", chat.Model, response.Messages.Count);
                        // if the last message had a FunctionCallContent then we need to resubmit the chat
                        if (response.Messages.LastOrDefault()?.Contents.OfType<FunctionCallContent>().Any() == true
                            //|| response.Messages.LastOrDefault()?.Contents.OfType<FunctionResultContent>().Any() == true)
                            )
                        {
                            _logger.LogInformation("Chat response contains a tool call for model {Model}, processing function call.", chat.Model);

                            // Find the assistant message with function calls that we just added
                            var assistantMessageWithFunctionCalls = chat.Messages
                                .Where(m => m.ApiChatMessage.Role == ChatRole.Assistant)
                                .OrderByDescending(m => m.Index)
                                .FirstOrDefault(m => m.ApiChatMessage.Contents.OfType<FunctionCallContent>().Any());

                            foreach (var functionCallContent in response.Messages.Last().Contents.OfType<FunctionCallContent>())
                            {

                                if (functionCallContent != null)
                                {
                                    pendingToolCall = true;
                                    _logger.LogInformation("Processing function call: {FunctionName}", functionCallContent.Name);
                                    // Process the function call

                                    var functionResult = await ProcessFunctionCallAsync(functionCallContent, chat.Options.ToolToServerMapping);
                                    _logger.LogInformation("Function call processed with result: {Result}", functionResult.Result);
                                    var functionResultContent = new FunctionResultContent(functionCallContent.CallId, functionResult.Result);
                                    // Add the function result to the chat
                                    var toolMsg = await AddMessageToChatAsync(chat, new ChatMessage(ChatRole.Tool, functionResultContent.Result?.ToString() ?? string.Empty)
                                    {
                                        Contents = [functionResultContent]
                                    }, []);
                                    
                                    // Mark the tool result message as completed
                                    toolMsg.IsCompletedToolResult = true;
                                    await PersistNewMessageToChat(chat, toolMsg);

                                }
                                else
                                {
                                    _logger.LogWarning("No function call content found in the last message of the response for model {Model}", chat.Model);
                                }
                            }
                            
                            // Mark the assistant message with function calls as completed after all tool results are processed
                            if (assistantMessageWithFunctionCalls != null)
                            {
                                await MarkFunctionCallAsCompleted(chat.Id, assistantMessageWithFunctionCalls.Id);
                            }
                        }
                        else
                        {
                            pendingToolCall = false;
                            _logger.LogInformation("Chat response does not contain a tool call, finalizing chat for model {Model}", chat.Model);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Received null response from AI chat for model {Model}", chat.Model);
                    }
                } while (pendingToolCall);

                return chat;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting chat for model {Model}", chat.Model);
                throw;
            }
        }

        private IChatClient GetChatClientAsync(Guid serverId, string model)
        {
            var db = _dbContextFactory.CreateDbContext();
            var server = db.ChatServers.FirstOrDefault(s => s.Id == serverId);
            if (server == null)
            {
                _logger.LogError("Chat server with ID {ServerId} not found.", serverId);
                throw new InvalidOperationException($"Chat server with ID {serverId} not found.");
            }
            _logger.LogInformation("Creating AI chat client for model {Model} on server {ServerName}", model, server.Name);
            var client = _clientFactory.CreateOpenClient(server.Uri, model);
            if (client == null)
            {
                _logger.LogError("Failed to create AI chat client for model {Model} on server {ServerName}", model, server.Name);
                throw new InvalidOperationException($"Failed to create AI chat client for model {model} on server {server.Name}");
            }
            _logger.LogInformation("AI chat client created successfully for model {Model} on server {ServerName}", model, server.Name);
            return client;
        }

        private async Task<FunctionResultContent> ProcessFunctionCallAsync(FunctionCallContent functionCallContent, Dictionary<string, Guid> toolToServerMapping)
        {
            _logger.LogInformation("Executing tool call: {FunctionName} with Arguments: {Arguments}",
                functionCallContent.Name,
                functionCallContent.Arguments != null ? JsonSerializer.Serialize(functionCallContent.Arguments) : "null");

            try
            {
                // Get the MCP server ID from the tool-to-server mapping
                var serverId = toolToServerMapping.TryGetValue(functionCallContent.Name, out var serverIdValue)
                    ? serverIdValue
                    : Guid.Empty;
                
                if (serverId == Guid.Empty)
                {
                    _logger.LogError("No MCP server found for tool: {ToolName}", functionCallContent.Name);
                    throw new InvalidOperationException($"No MCP server found for tool: {functionCallContent.Name}");
                }
                
                var mcpClient = await _mcpService.GetClientForServer(serverId);
                var result = await mcpClient.CallToolAsync(
                    functionCallContent.Name, 
                    functionCallContent.Arguments?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
                );

                _logger.LogInformation("Tool call {FunctionName} completed successfully. Result: {Result}",
                    functionCallContent.Name, result);

                return new FunctionResultContent(functionCallContent.CallId, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling tool {ToolName}: {Error}", functionCallContent.Name, ex.Message);
                return new FunctionResultContent(functionCallContent.CallId, $"Error: {ex.Message}");
            }
        }
        private async Task ProcessChatResponse(AppChat chat, ChatResponse response)
        {
            //if (response.Messages == null || !response.Messages.Any())
            //{
            //    _logger.LogWarning("No messages returned in chat response for model {Model}", chat.Model);
            //    return null;
            //}
            // Process each message in the response
            //AppChatMessage? managedMessage = default;
            foreach (var msg in response.Messages)
            {
                var m = await AddMessageToChatAsync(chat, msg, []);
                await PersistNewMessageToChat(chat, m);
            }

            //return managedMessage;
        }

        /// <summary>
        /// Adds a ChatMessage to the managed chat and returns the created AppChatMessage
        /// </summary>
        private async Task<AppChatMessage> AddMessageToChatAsync(AppChat chat, ChatMessage chatMessage, List<string>? imageUrls)
        {
            // Get the next index from the database to avoid race conditions
            int nextIndex = await GetNextMessageIndexAsync(chat.Id);
            
            // Convert imageUrls to AppChatImage objects
            var images = new List<AppChatImage>();
            if (imageUrls != null && imageUrls.Any())
            {
                _logger.LogInformation("Processing {Count} image URLs: {ImageUrls}", imageUrls.Count, string.Join(", ", imageUrls));
                foreach (var imageUrl in imageUrls)
                {
                    if (!string.IsNullOrEmpty(imageUrl))
                    {
                        _logger.LogInformation("Adding image: {ImageUrl}", imageUrl);
                        images.Add(new AppChatImage
                        {
                            Url = imageUrl,
                            Path = imageUrl, // Store as both URL and Path for compatibility
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }
                _logger.LogInformation("Created {Count} AppChatImage objects", images.Count);
            }
            else
            {
                _logger.LogInformation("No imageUrls provided or imageUrls list is empty");
            }
            
            var managedMessage = new AppChatMessage
            {
                Index = nextIndex,
                ApiChatMessage = chatMessage,
                // Set individual properties from ChatMessage
                Role = chatMessage.Role,
                Content = chatMessage.Contents.OfType<TextContent>().FirstOrDefault()?.Text,
                Images = images, // Use the new Images structure
#pragma warning disable CS0618 // Type or member is obsolete - Will be deprecated, TODO: Remove and reimplement
                ImageUrls = imageUrls ?? new List<string>(), // Keep for backward compatibility
#pragma warning restore CS0618
                CreatedAt = DateTime.UtcNow,
            };
            chat.Messages.Add(managedMessage);
            return managedMessage;
        }

        /// <summary>
        /// Gets the next message index for a chat from the database
        /// </summary>
        private async Task<int> GetNextMessageIndexAsync(Guid chatId)
        {
            using var db = _dbContextFactory.CreateDbContext();
            var maxIndex = await db.AppChatMessages
                .Where(m => m.AppChatEntityId == chatId)
                .Select(m => (int?)m.Index)
                .MaxAsync();
            return (maxIndex ?? -1) + 1;
        }

        private async Task PersistNewMessageToChat(AppChat chat, AppChatMessage managedMessage)
        {
            using var db = _dbContextFactory.CreateDbContext();

            var existingChat = await db.AppChats.Include(c => c.Messages).FirstOrDefaultAsync(c => c.Id == chat.Id);
            if (existingChat == null)
            {
                _logger.LogError("Chat with ID {ChatId} not found in the database.", chat.Id);
                throw new InvalidOperationException($"Chat with ID {chat.Id} not found.");
            }

            existingChat.Messages.Add(managedMessage.ToEntity());
            existingChat.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }

        /// <summary>
        /// Marks a function call message as completed in the database
        /// </summary>
        private async Task MarkFunctionCallAsCompleted(Guid chatId, Guid messageId)
        {
            using var db = _dbContextFactory.CreateDbContext();
            Data.Entities.AppMessageEntity? message = await db.AppChatMessages
                .Where(m => m.AppChatEntityId == chatId && m.Id == messageId)
                .FirstOrDefaultAsync();
            
            if (message != null)
            {
                message.IsCompletedFunctionCall = true;
                await db.SaveChangesAsync();
                _logger.LogDebug("Marked message {MessageId} as completed function call", messageId);
            }
        }

        /// <summary>
        /// Converts managed chat messages to AI chat messages for submission.
        /// Optionally excludes messages marked as completed function calls or tool results to prevent replaying.
        /// </summary>
        private IEnumerable<ChatMessage> GetChatMessageStream(AppChat chat)
        {
            var query = chat.Messages.AsQueryable();
            
            if (_options.ExcludeCompletedFunctionCalls)
            {
                query = query.Where(m => !m.IsCompletedFunctionCall 
               // && !m.IsCompletedToolResult
                );
            }
            
            return query
                .OrderBy(m => m.Index)
                .Select(m => m.ApiChatMessage);
        }

        /// <summary>
        /// Creates chat options for AI client
        /// </summary>
        private static ChatOptions CreateChatOptions(AppChat chat, ModelCapabilities modelCapabilities, IList<ModelContextProtocol.Client.McpClientTool> tools)
        {
            var options = new ChatOptions
            {
                AllowMultipleToolCalls = chat.Options.DisableMultipleToolCalls,
                Temperature = chat.Options.Temperature,
                ToolMode = modelCapabilities.SupportsTools ? ChatToolMode.Auto : ChatToolMode.None,

                TopP = chat.Options.TopP,
                FrequencyPenalty = chat.Options.FrequencyPenalty,
                PresencePenalty = chat.Options.PresencePenalty,
                Instructions = chat.Messages
                    .Where(m => m.ApiChatMessage.Role == ChatRole.System)
                    .Select(m => m.ApiChatMessage.Text)
                    .FirstOrDefault() ?? string.Empty,

            };

            if (modelCapabilities.SupportsTools)
            {
                options.Tools = [.. tools];
            }

            return options;
        }

        /// <summary>
        /// Determines which tools should be enabled for the chat
        /// </summary>
        private static IList<ModelContextProtocol.Client.McpClientTool> DetermineEnabledTools(AppChat chat,
            IList<ModelContextProtocol.Client.McpClientTool> availableTools)
        {
            // First check if specific tool IDs were enabled (from tool selection modal)
            if (chat.Options.EnabledToolIds is not null && chat.Options.EnabledToolIds.Any())
            {
                // The availableTools already filtered by EnabledToolIds in SubmitChatAsync, so return all available
                return availableTools.ToList();
            }

            // Fallback to check tool names (legacy support)
            if (chat.Options.EnabledTools is not null && chat.Options.EnabledTools.Any())
            {
                return availableTools.Where(t => chat.Options.EnabledTools.Contains(t.Name)).ToList();
            }

            // Find the last user message to determine enabled tools (if any)
            var lastUserMessage = chat.Messages
                .Where(m => m.ApiChatMessage.Role == ChatRole.User)
                .OrderByDescending(m => m.Index)
                .FirstOrDefault();

            // If no tools were specifically selected, don't enable any tools by default
            return new List<ModelContextProtocol.Client.McpClientTool>();
        }

        /// <summary>
        /// Deletes a managed chat
        /// </summary>
        public async Task DeleteChatAsync(Guid id)
        {
            using var db = _dbContextFactory.CreateDbContext();
            var chat = await db.AppChats.FirstOrDefaultAsync(c => c.Id == id);
            if (chat != null)
            {
                db.AppChats.Remove(chat);
                await db.SaveChangesAsync();
            }
        }

        /// <summary>
        /// <summary>
        /// Lists all managed chats
        /// </summary>
        public async Task<List<AppChat>> ListChatsAsync(int skip = 0, int take = 50)
        {
            using var db = _dbContextFactory.CreateDbContext();
            var chats = await db.AppChats
                .Include(c => c.Messages)
                .OrderByDescending(c => c.UpdatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
            return chats.Select(AppChat.FromEntity).ToList();
        }

        /// <summary>
        /// Lists managed chats for a specific server
        /// </summary>
        public async Task<List<AppChat>> ListChatsAsync(Guid serverId, int skip = 0, int take = 50)
        {
            using var db = _dbContextFactory.CreateDbContext();
            var chats = await db.AppChats
                .Include(c => c.Messages)
                .Where(c => c.ChatServerId == serverId)
                .OrderByDescending(c => c.UpdatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
            return chats.Select(AppChat.FromEntity).ToList();
        }

        /// <summary>
        /// Gets the conversation history as text for display purposes
        /// </summary>
        public string GetConversationText(AppChat chat)
        {
            var conversation = new List<string>();

            foreach (var message in chat.Messages.OrderBy(m => m.Index))
            {
                var role = message.ApiChatMessage.Role.ToString();
                var content = message.ApiChatMessage.Text;

                if (!string.IsNullOrEmpty(content))
                {
                    conversation.Add($"{role}: {content}");
                }
            }

            return string.Join("\n\n", conversation);
        }

        public async Task DeleteMessageAsync(Guid messageId)
        {
            using var db = _dbContextFactory.CreateDbContext();
            var message = db.AppChatMessages.FirstOrDefault(m => m.Id == messageId);
            if (message != null)
            {
                db.AppChatMessages.Remove(message);
                await db.SaveChangesAsync();
            }
        }

        public async Task CreateModelFromChatAsync(Guid id, ModelRequest request)
        {
            // TODO: Implement model creation from chat
            await Task.CompletedTask;
            throw new NotImplementedException();
        }

        public async Task UpdateChatOptions(Guid id, AppChatOptions options)
        {
            using var db = _dbContextFactory.CreateDbContext();
            var chat = await db.AppChats.FirstOrDefaultAsync(c => c.Id == id);
            if (chat != null)
            {
                chat.Options = options.ToEntity();
                chat.UpdatedAt = DateTime.UtcNow;
                db.AppChats.Update(chat);
                await db.SaveChangesAsync();
            }
            else
            {
                _logger.LogWarning("Chat with ID {ChatId} not found for updating options.", id);
            }
        }

        /// <summary>
        /// Resets completion status for all messages in a chat (useful for debugging or re-processing)
        /// </summary>
        public async Task ResetFunctionCallCompletionStatus(Guid chatId)
        {
            using var db = _dbContextFactory.CreateDbContext();
            var messages = await db.AppChatMessages
                .Where(m => m.AppChatEntityId == chatId)
                .ToListAsync();
            
            foreach (var message in messages)
            {
                message.IsCompletedFunctionCall = false;
                message.IsCompletedToolResult = false;
            }
            
            await db.SaveChangesAsync();
            _logger.LogInformation("Reset function call completion status for {MessageCount} messages in chat {ChatId}", 
                messages.Count, chatId);
        }

        public async Task UpdateMessageAsync(Guid messageId, ChatRole role, string message)
        {
            using var db = _dbContextFactory.CreateDbContext();
            var managedMessage = db.AppChatMessages.FirstOrDefault(m => m.Id == messageId);
            if (managedMessage != null)
            {
                // TODO: This method needs to be refactored to work with the new domain/entity separation
                // The ApiChatMessage is now a Guid reference, not the actual object
                // For now, we'll update the content directly
                managedMessage.Content = message;
                await db.SaveChangesAsync();
            }
        }
    }
}
