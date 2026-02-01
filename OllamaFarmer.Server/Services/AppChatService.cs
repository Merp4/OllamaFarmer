using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using OllamaFarmer.Data;
using OllamaFarmer.Data.Entities;
using ServerAppChat = OllamaFarmer.Server.Models.AppChat;
using ServerAppChatOptions = OllamaFarmer.Server.Models.AppChatOptions;
using ServerAppChatMessage = OllamaFarmer.Server.Models.AppChatMessage;
using ServerAppChatImage = OllamaFarmer.Server.Models.AppChatImage;
using OllamaFarmer.Server.Services.Interfaces;
using OllamaFarmer.Server.Dto;
using OllamaFarmer.Server.Services.Mcp;
using OllamaFarmer.Server.Services.Ollama;
using OllamaFarmer.Server.Services.Ollama.Interfaces;
using OllamaFarmer.Abstractions.AI;
using OllamaSharp.ModelContextProtocol.Server;
using System.Text.Json;
using OllamaFarmer.Server.Data;
using Microsoft.AspNetCore.SignalR;
using OllamaFarmer.SignalR;
using System.Collections;
using System.Reflection;

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

    /// <summary>
    /// When true, the most recent assistant function-call message is included in the prompt context
    /// even if it's marked completed. This helps the model relate the subsequent tool result to the
    /// originating call without re-exposing older calls. Default: true
    /// </summary>
    public bool IncludeLastFunctionCallInContext { get; set; } = true;
    }

    /// <summary>
    /// This class aims to provide a managed chat service for handling chat operations.
    /// Replacing the previous IChatClientService, it will manage chat creation, message handling, persistence/state management, and other chat-related functionalities.
    /// This will use the non streaming endpoints for simplicity.
    /// </summary>
    public class AppChatService : IAppChatService
    {
    private readonly OllamaFarmer.Abstractions.AI.IChatClientFactory _clientFactory;
    private readonly IDbContextFactory<OllamaFarmer.Data.AppDbContext> _dbContextFactory;
        private readonly ILogger<AppChatService> _logger;
        private readonly IBinaryRepository _binaryRepository;
        private readonly IMcpToolService _mcpService;
        private readonly IOllamaModelService _ollamaModelService;
    private readonly AppChatServiceOptions _options;
    private readonly IHubContext<NotificationHub> _hub;
        private const string FunctionCallContent_AdditionalProperties_Key_McpServerId = "FunctionCallContent_AdditionalProperties_Key_McpServerId";

        public AppChatService(
            OllamaFarmer.Abstractions.AI.IChatClientFactory clientFactory,
            IDbContextFactory<OllamaFarmer.Data.AppDbContext> dbContextFactory,
            ILogger<AppChatService> logger,
            IBinaryRepository binaryRepository,
            IMcpToolService mcpService,
            IOllamaModelService ollamaModelService,
            AppChatServiceOptions? options,
            IHubContext<NotificationHub> hub)
        {
            _clientFactory = clientFactory;
            _dbContextFactory = dbContextFactory;
            _logger = logger;
            _binaryRepository = binaryRepository;
            _mcpService = mcpService;
            _ollamaModelService = ollamaModelService;
            _options = options ?? new AppChatServiceOptions();
            _hub = hub;
        }

        private record ChatEvent(
            string Type,
            Guid ChatId,
            object Payload,
            DateTime Timestamp
        );

        /// <summary>
        /// Creates a new managed chat
        /// </summary>
    public async Task<ServerAppChat> CreateChatAsync(Guid serverId, string? name = null, string? model = null, string? systemMessage = null)
        {
            return await CreateChatAsync(serverId, name, model, systemMessage, new());
        }

    public async Task<ServerAppChat> CreateChatAsync(Guid serverId, string? name = null, string? model = null, string? systemMessage = null, ServerAppChatOptions? options = null)
        {
            options ??= new();

            var chat = new ServerAppChat
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
    public async Task<ServerAppChat?> GetChatAsync(Guid id)
        {
            using var db = _dbContextFactory.CreateDbContext();
            var entity = await db.AppChats
                //.ThenInclude(o => o.)
                .Include(c => c.Messages)
                .FirstOrDefaultAsync(c => c.Id == id);
            return entity != null ? ServerAppChat.FromEntity(entity) : null;
        }

        /// <summary>
        /// Saves a managed chat to persistence
        /// </summary>
    public async Task SaveChatAsync(ServerAppChat chat)
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
    /// /// </summary>
    public async Task<ServerAppChat> AddUserMessageAsync(ServerAppChat chat, string content, List<string>? imageUrls = null)
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
    public async Task<ServerAppChat> AddMessageAsync(ServerAppChat chat, string content, ChatRole role, List<string>? imageUrls = null)
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
        /// Convenience method used by MCP tools to get a single-shot response without managing chat state externally.
        /// </summary>
        public async Task<string> GenerateResponseAsync(Guid serverId, string model, string systemMessage, string userMessage, float temperature, float topP, float frequencyPenalty, float presencePenalty)
        {
            var chat = await CreateChatAsync(serverId, name: $"{model} Quick Chat", model: model, systemMessage: systemMessage);
            await AddUserMessageAsync(chat, userMessage);
            var result = await SubmitChatAsync(serverId, chat);
            var assistant = result.Messages.LastOrDefault(m => m.ApiChatMessage.Role == ChatRole.Assistant)?.ApiChatMessage?.Contents?.FirstOrDefault()?.ToString();
            return assistant ?? string.Empty;
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

    private async Task<Guid[]> ExpandEnabledToolIdsAsync(ServerAppChat chat)
        {
            using var db = _dbContextFactory.CreateDbContext();

            var directToolIds = chat.Options.EnabledToolIds?.ToList() ?? new List<Guid>();

            if (chat.Options.EnabledToolBagIds is { Count: > 0 })
            {
                var bagIds = chat.Options.EnabledToolBagIds;
                var bagToolIds = await db.ToolBagTools
                    .Where(t => bagIds.Contains(t.ToolBagId))
                    .Select(t => t.McpToolId)
                    .ToListAsync();

                directToolIds.AddRange(bagToolIds);
            }

            return directToolIds.Distinct().ToArray();
        }

        /// <summary>
        /// Submits a chat and gets AI response - returns the assistant's response message
        /// </summary>
    public async Task<ServerAppChat> SubmitChatAsync(Guid serverId, ServerAppChat chat)
        {
            chat.Model = chat.Model ?? _options.DefaultModel;

            var modelCapabilities = await _ollamaModelService.GetModelCapabilitiesAsync(serverId, chat.Model);
            //var mcp = await _mcpService.GetMcpClientAsync(serverId);
            var aiChat = GetChatClientAsync(serverId, chat.Model);
            // MCP McpClient Tool AIFunction

            _logger.LogInformation("Requested tools: {ToolIds}", string.Join(", ", chat.Options.EnabledToolIds));

            // Expand EnabledToolBagIds into tool IDs
            var expandedToolIds = await ExpandEnabledToolIdsAsync(chat);

            var availableTools = await _mcpService.GetClientToolsAsync(expandedToolIds);
            _logger.LogInformation("{Model} available tools: {Tools}", chat.Model, string.Join(", ", availableTools.Select(t => t.Name)));

            // Create a mapping from tool name to MCP server ID for later use during tool execution
            var toolToServerMapping = await CreateToolToServerMapping(expandedToolIds);
            chat.Options.ToolToServerMapping = toolToServerMapping;

            var enabledTools = DetermineEnabledTools(chat, availableTools);
            _logger.LogInformation("{Model} enabled tools: {Tools}", chat.Model, string.Join(", ", enabledTools.Select(t => t.Name)));

            var chatOptions = CreateChatOptions(chat, modelCapabilities ?? new OllamaFarmer.Data.Entities.ModelCapabilities(), enabledTools);
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
                        if (response.Messages.LastOrDefault()?.Contents.OfType<FunctionCallContent>().Any() == true)
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

                                    // Broadcast: function call started
                                    try
                                    {
                                        var evtStart = new ChatEvent(
                                            Type: "functionCallStarted",
                                            ChatId: chat.Id,
                                            Payload: new
                                            {
                                                callId = functionCallContent.CallId,
                                                name = functionCallContent.Name,
                                                args = functionCallContent.Arguments
                                            },
                                            Timestamp: DateTime.UtcNow
                                        );
                                        await _hub.Clients.Group(chat.Id.ToString()).SendAsync("chat:event", evtStart);
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.LogWarning(ex, "Failed to broadcast functionCallStarted for chat {ChatId}", chat.Id);
                                    }

                                    var functionResult = await ProcessFunctionCallAsync(functionCallContent, chat.Options.ToolToServerMapping);
                                    var resultPreview = FormatToolResultForDisplay(functionResult.Result);
                                    _logger.LogInformation("Function call processed. Preview: {Preview}", string.IsNullOrWhiteSpace(resultPreview) ? "<empty>" : resultPreview);
                                    var functionResultContent = new FunctionResultContent(functionCallContent.CallId, functionResult.Result);
                                    // Add the function result to the chat with a human-readable/Text or JSON string
                                    var toolMsg = await AddMessageToChatAsync(
                                        chat,
                                        new ChatMessage(ChatRole.Tool, resultPreview)
                                        {
                                            Contents = [functionResultContent]
                                        },
                                        []);
                                    
                                    // Mark the tool result message as completed
                                    toolMsg.IsCompletedToolResult = true;
                                    await PersistNewMessageToChat(chat, toolMsg);

                                    // Broadcast: tool result added
                                    try
                                    {
                                        var evtResult = new ChatEvent(
                                            Type: "toolResultAdded",
                                            ChatId: chat.Id,
                                            Payload: new
                                            {
                                                callId = functionCallContent.CallId,
                                                name = functionCallContent.Name,
                                                message = new AppMessageDto(toolMsg, chat.Options.EnabledToolIds?.ToList() ?? new List<Guid>())
                                            },
                                            Timestamp: DateTime.UtcNow
                                        );
                                        await _hub.Clients.Group(chat.Id.ToString()).SendAsync("chat:event", evtResult);
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.LogWarning(ex, "Failed to broadcast toolResultAdded for chat {ChatId}", chat.Id);
                                    }

                                }
                                else
                                {
                                    _logger.LogWarning("No function call content found in the last message of the response for model {Model}", chat.Model);
                                }
                            }
                            
                            // Mark the assistant message with function calls as completed after all tool results are processed
                            if (assistantMessageWithFunctionCalls != null)
                            {
                                // Mark completed in-memory to keep the stream filter consistent this iteration
                                assistantMessageWithFunctionCalls.IsCompletedFunctionCall = true;
                                await MarkFunctionCallAsCompleted(chat.Id, assistantMessageWithFunctionCalls.Id);
                            }
                        }
                        else
                        {
                            pendingToolCall = false;
                            _logger.LogInformation("Chat response does not contain a tool call, finalizing chat for model {Model}", chat.Model);
                            // Broadcast: assistant response finalized without tool call
                            try
                            {
                                var evtDone = new ChatEvent(
                                    Type: "assistantResponseFinalized",
                                    ChatId: chat.Id,
                                    Payload: new { },
                                    Timestamp: DateTime.UtcNow
                                );
                                await _hub.Clients.Group(chat.Id.ToString()).SendAsync("chat:event", evtDone);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Failed to broadcast assistantResponseFinalized for chat {ChatId}", chat.Id);
                            }
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
        /// <summary>
        /// Attempts to create a readable string for tool results. Handles strings, JsonElements,
        /// ModelContextProtocol.Protocol.CallToolResult (via reflection), and falls back to JSON serialization.
        /// </summary>
        private static string FormatToolResultForDisplay(object? result)
        {
            if (result is null)
            {
                return string.Empty;
            }
            if (result is string s)
            {
                return s;
            }
            if (result is JsonElement je)
            {
                return je.ValueKind == JsonValueKind.String ? (je.GetString() ?? string.Empty) : je.GetRawText();
            }

            // Try to extract text segments from known MCP result shapes via reflection
            try
            {
                var text = ExtractTextFromResultContent(result);
                if (!string.IsNullOrWhiteSpace(text))
                {
                    return text;
                }
            }
            catch
            {
                // ignore and fall through to JSON serialization
            }

            // Fallback: compact JSON serialization
            try
            {
                return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = false });
            }
            catch
            {
                return result.ToString() ?? string.Empty;
            }
        }

        /// <summary>
        /// Uses reflection to find a "Content"/"Contents" enumerable and concatenate any "Text" fields.
        /// This matches typical MCP CallToolResult { content: [ { type: "text", text: "..." }, ... ] } shapes
        /// and also .NET classes with TextContent objects.
        /// </summary>
        private static string ExtractTextFromResultContent(object result)
        {
            var type = result.GetType();
            // Find a property named Content or Contents (case-insensitive)
            var contentProp = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(p => string.Equals(p.Name, "Content", StringComparison.OrdinalIgnoreCase)
                                   || string.Equals(p.Name, "Contents", StringComparison.OrdinalIgnoreCase));
            if (contentProp == null)
            {
                return string.Empty;
            }

            var contentVal = contentProp.GetValue(result);
            if (contentVal is null || contentVal is string)
            {
                return contentVal as string ?? string.Empty;
            }

            if (contentVal is IEnumerable enumerable)
            {
                var pieces = new List<string>();
                foreach (var item in enumerable)
                {
                    if (item is null) continue;
                    var itemType = item.GetType();

                    // Prefer Text property
                    var textProp = itemType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                        .FirstOrDefault(p => string.Equals(p.Name, "Text", StringComparison.OrdinalIgnoreCase));
                    if (textProp != null)
                    {
                        var tv = textProp.GetValue(item);
                        if (tv is string ts && !string.IsNullOrWhiteSpace(ts))
                        {
                            pieces.Add(ts);
                            continue;
                        }
                    }

                    // If it has a 'type' property equal to 'text' and a 'text' value
                    var typeProp = itemType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                        .FirstOrDefault(p => string.Equals(p.Name, "Type", StringComparison.OrdinalIgnoreCase));
                    var textValProp = itemType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                        .FirstOrDefault(p => string.Equals(p.Name, "Text", StringComparison.OrdinalIgnoreCase)
                                           || string.Equals(p.Name, "Value", StringComparison.OrdinalIgnoreCase));
                    var typeVal = typeProp?.GetValue(item)?.ToString();
                    var textVal = textValProp?.GetValue(item)?.ToString();
                    if (string.Equals(typeVal, "text", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(textVal))
                    {
                        pieces.Add(textVal);
                        continue;
                    }

                    // Last resort for this piece: JSON-serialize the item
                    try
                    {
                        pieces.Add(JsonSerializer.Serialize(item, new JsonSerializerOptions { WriteIndented = false }));
                    }
                    catch
                    {
                        var s = item.ToString();
                        if (!string.IsNullOrWhiteSpace(s)) pieces.Add(s);
                    }
                }

                return string.Join("\n", pieces.Where(p => !string.IsNullOrWhiteSpace(p)));
            }

            return string.Empty;
        }
    private async Task ProcessChatResponse(ServerAppChat chat, ChatResponse response)
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
    private async Task<ServerAppChatMessage> AddMessageToChatAsync(ServerAppChat chat, ChatMessage chatMessage, List<string>? imageUrls)
        {
            // Get the next index from the database to avoid race conditions
            int nextIndex = await GetNextMessageIndexAsync(chat.Id);
            
            // Convert imageUrls to AppChatImage objects
            var images = new List<ServerAppChatImage>();
            if (imageUrls != null && imageUrls.Any())
            {
                _logger.LogInformation("Processing {Count} image URLs: {ImageUrls}", imageUrls.Count, string.Join(", ", imageUrls));
                foreach (var imageUrl in imageUrls)
                {
                    if (!string.IsNullOrEmpty(imageUrl))
                    {
                        _logger.LogInformation("Adding image: {ImageUrl}", imageUrl);
                        images.Add(new ServerAppChatImage
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
            
            var managedMessage = new ServerAppChatMessage
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

            // Fallbacks: ensure Content is set for tool results even if TextContent isn't present
            if (string.IsNullOrWhiteSpace(managedMessage.Content))
            {
                // Prefer ChatMessage.Text if available
                managedMessage.Content = chatMessage.Text;

                // If still empty, and a FunctionResultContent exists, use its Result string
                if (string.IsNullOrWhiteSpace(managedMessage.Content))
                {
                    var fr = chatMessage.Contents.OfType<FunctionResultContent>().FirstOrDefault();
                    if (fr?.Result != null)
                    {
                        managedMessage.Content = FormatToolResultForDisplay(fr.Result);
                    }
                }
            }

            // If the message contains a tool/function call, capture its metadata for UI/history
            var func = chatMessage.Contents.OfType<FunctionCallContent>().FirstOrDefault();
            if (func != null)
            {
                managedMessage.ToolCallId = func.CallId;
                managedMessage.ToolCallName = func.Name;
                managedMessage.ToolCallArgs = func.Arguments?.ToDictionary(kv => kv.Key, kv => kv.Value);
                managedMessage.IsCompletedFunctionCall = false; // will be marked true when processed
                managedMessage.IsCompletedToolResult = false;
            }
            // If the message is a tool result, capture the call id so UI can match it to the originating call
            var funcResult = chatMessage.Contents.OfType<FunctionResultContent>().FirstOrDefault();
            if (funcResult != null)
            {
                managedMessage.ToolCallId = funcResult.CallId;
                // Tool result may not carry the tool name; leave ToolCallName unchanged
            }
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

    private async Task PersistNewMessageToChat(ServerAppChat chat, ServerAppChatMessage managedMessage)
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

            // Broadcast to chat group: message added
            try
            {
                var evt = new ChatEvent(
                    Type: "messageAdded",
                    ChatId: chat.Id,
                    Payload: new
                    {
                        message = new AppMessageDto(managedMessage, chat.Options.EnabledToolIds?.ToList() ?? new List<Guid>())
                    },
                    Timestamp: DateTime.UtcNow
                );
                await _hub.Clients.Group(chat.Id.ToString()).SendAsync("chat:event", evt);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to broadcast messageAdded for chat {ChatId}", chat.Id);
            }
        }

        /// <summary>
        /// Marks a function call message as completed in the database
        /// </summary>
        private async Task MarkFunctionCallAsCompleted(Guid chatId, Guid messageId)
        {
            using var db = _dbContextFactory.CreateDbContext();
            var message = await db.AppChatMessages
                .Where(m => m.AppChatEntityId == chatId && m.Id == messageId)
                .FirstOrDefaultAsync();
            
            if (message != null)
            {
                message.IsCompletedFunctionCall = true;
                await db.SaveChangesAsync();
                _logger.LogDebug("Marked message {MessageId} as completed function call", messageId);

                // Broadcast to chat group: function call started/completed
                try
                {
                    var evt = new ChatEvent(
                        Type: "functionCallCompleted",
                        ChatId: chatId,
                        Payload: new { messageId },
                        Timestamp: DateTime.UtcNow
                    );
                    await _hub.Clients.Group(chatId.ToString()).SendAsync("chat:event", evt);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to broadcast functionCallCompleted for chat {ChatId}", chatId);
                }
            }
        }

        /// <summary>
        /// Converts managed chat messages to AI chat messages for submission.
        /// Optionally excludes messages marked as completed function calls or tool results to prevent replaying.
        /// </summary>
    private IEnumerable<ChatMessage> GetChatMessageStream(ServerAppChat chat)
        {
            var msgs = chat.Messages.AsQueryable();

            if (_options.ExcludeCompletedFunctionCalls)
            {
                // Optionally include only the latest assistant function-call message to pair with its tool result
                Guid keepLastCallId = Guid.Empty;
                if (_options.IncludeLastFunctionCallInContext)
                {
                    var lastAssistantCall = chat.Messages
                        .Where(m => m.ApiChatMessage.Role == ChatRole.Assistant
                                    && m.ApiChatMessage.Contents.OfType<FunctionCallContent>().Any())
                        .OrderByDescending(m => m.Index)
                        .FirstOrDefault();
                    if (lastAssistantCall != null)
                    {
                        keepLastCallId = lastAssistantCall.Id;
                    }
                }

                msgs = msgs.Where(m => !m.IsCompletedFunctionCall || m.Id == keepLastCallId);
            }

            return msgs
                .OrderBy(m => m.Index)
                .Select(m => m.ApiChatMessage);
        }

        /// <summary>
        /// Creates chat options for AI client
        /// </summary>
    private static ChatOptions CreateChatOptions(ServerAppChat chat, OllamaFarmer.Data.Entities.ModelCapabilities modelCapabilities, IList<ModelContextProtocol.Client.McpClientTool> tools)
        {
            var options = new ChatOptions
            {
                // Invert the Disable flag to the Allow flag expected by ChatOptions
                AllowMultipleToolCalls = !chat.Options.DisableMultipleToolCalls,
                Temperature = chat.Options.Temperature,
                // Default to no tools unless explicitly enabled and supported
                ToolMode = ChatToolMode.None,

                TopP = chat.Options.TopP,
                FrequencyPenalty = chat.Options.FrequencyPenalty,
                PresencePenalty = chat.Options.PresencePenalty,
                Instructions = chat.Messages
                    .Where(m => m.ApiChatMessage.Role == ChatRole.System)
                    .Select(m => m.ApiChatMessage.Text)
                    .FirstOrDefault() ?? string.Empty,

            };

            // Only expose tools to the model when the model supports tools AND there are tools selected/enabled.
            if (modelCapabilities.SupportsTools && tools is { Count: > 0 })
            {
                options.ToolMode = ChatToolMode.Auto;
                options.Tools = [.. tools];
            }

            return options;
        }

        /// <summary>
        /// Determines which tools should be enabled for the chat
        /// </summary>
    private static IList<ModelContextProtocol.Client.McpClientTool> DetermineEnabledTools(ServerAppChat chat,
            IList<ModelContextProtocol.Client.McpClientTool> availableTools)
        {
            // If any explicit tool IDs are present, return all available (already filtered upstream)
            if (chat.Options.EnabledToolIds is not null && chat.Options.EnabledToolIds.Any())
            {
                return availableTools.ToList();
            }

            // If any tool bags are selected, return all available (already expanded upstream)
            if (chat.Options.EnabledToolBagIds is not null && chat.Options.EnabledToolBagIds.Any())
            {
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
    public async Task<List<ServerAppChat>> ListChatsAsync(int skip = 0, int take = 50)
        {
            using var db = _dbContextFactory.CreateDbContext();
            var chats = await db.AppChats
                .Include(c => c.Messages)
                .OrderByDescending(c => c.UpdatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
            return chats.Select(OllamaFarmer.Server.Models.AppChat.FromEntity).ToList();
        }

        /// <summary>
        /// Lists managed chats for a specific server
        /// </summary>
    public async Task<List<ServerAppChat>> ListChatsAsync(Guid serverId, int skip = 0, int take = 50)
        {
            using var db = _dbContextFactory.CreateDbContext();
            var chats = await db.AppChats
                .Include(c => c.Messages)
                .Where(c => c.ChatServerId == serverId)
                .OrderByDescending(c => c.UpdatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
            return chats.Select(OllamaFarmer.Server.Models.AppChat.FromEntity).ToList();
        }

        /// <summary>
        /// Gets the conversation history as text for display purposes
        /// </summary>
    public string GetConversationText(ServerAppChat chat)
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

    public async Task UpdateChatOptions(Guid id, ServerAppChatOptions options)
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

        /// <summary>
        /// Clones an existing chat with all its messages and metadata
        /// </summary>
    public async Task<ServerAppChat> CloneChatAsync(Guid sourceChatId, string? newName = null)
        {
            var sourceChat = await GetChatAsync(sourceChatId);
            if (sourceChat == null)
            {
                throw new InvalidOperationException($"Source chat with ID {sourceChatId} not found.");
            }

            // Create a new chat with the same configuration
            var clonedChat = new ServerAppChat
            {
                Id = Guid.NewGuid(),
                ChatServerId = sourceChat.ChatServerId,
                Name = newName ?? $"Copy of {sourceChat.Name ?? "Unnamed Chat"}",
                Model = sourceChat.Model,
                Options = new ServerAppChatOptions
                {
                    Temperature = sourceChat.Options.Temperature,
                    TopP = sourceChat.Options.TopP,
                    FrequencyPenalty = sourceChat.Options.FrequencyPenalty,
                    PresencePenalty = sourceChat.Options.PresencePenalty,
                    EnabledToolIds = sourceChat.Options.EnabledToolIds?.ToList() ?? new List<Guid>(),
                    EnabledToolBagIds = sourceChat.Options.EnabledToolBagIds?.ToList() ?? new List<Guid>(),
                    EnabledTools = sourceChat.Options.EnabledTools?.ToList() ?? new List<string>(),
                    DisableMultipleToolCalls = sourceChat.Options.DisableMultipleToolCalls
                },
                ModelCapabilities = sourceChat.ModelCapabilities,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Clone all messages while preserving order and content
            foreach (var sourceMessage in sourceChat.Messages.OrderBy(m => m.Index))
            {
                var clonedMessage = new ServerAppChatMessage
                {
                    Id = Guid.NewGuid(),
                    Index = sourceMessage.Index,
                    Role = sourceMessage.Role,
                    Content = sourceMessage.Content,
                    CreatedAt = DateTime.UtcNow,
                    ApiChatMessage = new ChatMessage(sourceMessage.Role ?? ChatRole.User, sourceMessage.Content ?? string.Empty)
                };

                // Clone the contents of the API chat message
                if (sourceMessage.ApiChatMessage != null)
                {
                    clonedMessage.ApiChatMessage = new ChatMessage(sourceMessage.ApiChatMessage.Role, sourceMessage.ApiChatMessage.Text ?? string.Empty);
                    
                    // Copy additional content types (like images)
                    foreach (var content in sourceMessage.ApiChatMessage.Contents)
                    {
                        clonedMessage.ApiChatMessage.Contents.Add(content);
                    }
                }

                // Clone images - we reference the same URLs since images are stored separately
                foreach (var sourceImage in sourceMessage.Images)
                {
                    clonedMessage.Images.Add(new ServerAppChatImage
                    {
                        Id = Guid.NewGuid(),
                        Url = sourceImage.Url,
                        Path = sourceImage.Path,
                        CreatedAt = DateTime.UtcNow
                    });
                }

                // Copy image URLs for backward compatibility
                clonedMessage.ImageUrls = sourceMessage.ImageUrls?.ToList() ?? new List<string>();

                clonedChat.Messages.Add(clonedMessage);
            }

            // Save the cloned chat
            await SaveChatAsync(clonedChat);
            
            _logger.LogInformation("Successfully cloned chat {SourceChatId} to new chat {ClonedChatId} with name {Name}", 
                sourceChatId, clonedChat.Id, clonedChat.Name);

            return clonedChat;
        }
    }
}
