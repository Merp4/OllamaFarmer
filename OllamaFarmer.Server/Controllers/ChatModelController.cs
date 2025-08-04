using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using OllamaFarmer.Server.Data;
using OllamaFarmer.Server.Dto;
using OllamaFarmer.Server.Models;
using OllamaSharp.Models;
using OllamaSharp.Models.Chat;
using OllamaFarmer.Server.Services.Ollama.Interfaces;
using OllamaFarmer.Server.Services.SignalR;
using OllamaFarmer.Server.Services.Background.Interfaces;
using OllamaFarmer.Server.Services.Ollama;

namespace OllamaFarmer.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatModelController(
        IOllamaModelService ollamaService, 
        AppDbContext dbContext, 
        ILogger<ChatModelController> logger,
        IHubContext<NotificationHub> hubContext,
        IBackgroundTaskQueue taskQueue) : ControllerBase
    {

        [HttpGet("{id}")]
        public async Task<ActionResult<ChatModelDto>> GetChatModelAsync(Guid id)
        {
            var model = await dbContext.Set<ChatModelDto>().FirstOrDefaultAsync(x => x.Id == id);
            if (model == null)
                return NotFound();
            return Ok(model);
        }

        [HttpGet("details/{id}")]
        public async Task<ActionResult<ChatModelAndDetails>> GetModelDetailsAsync([FromQuery] Guid serverId, Guid id)
        {
            var model = await ollamaService.GetModelDetailsAsync(serverId, id);
            if (model == null)
                return NotFound();
            return Ok(model);
        }

        [HttpGet("all")]
        public async Task<IEnumerable<ChatModelDto>> GetAllModelsAsync([FromQuery] Guid serverId)
        {
            var models = await ollamaService.GetAllChatModelsAsync();
            return models;
        }

        [HttpGet("details/all")]
        public async Task<PagedResponse<IEnumerable<ChatModelAndDetails>>> GetAllModelDetailsAsync([FromQuery] Guid serverId, int cursor = 0, int pageSize = 10)
        {
            var models = (await ollamaService.GetAllModelDetailsAsync(serverId)).OrderBy(m => m.ChatModel.Name).ToList();
            return new PagedResponse<IEnumerable<ChatModelAndDetails>>()
            { Cursor = cursor, PageSize = pageSize, FilteredCount = models.Count, TotalCount = models.Count, Data = models.Skip(cursor * pageSize).Take(pageSize) };
        }

        [HttpGet("available")]
        public async Task<IEnumerable<Model>> GetAllAvailableApiModelsAsync([FromQuery] Guid serverId)
        {
            var models = await ollamaService.GetAvailableApiModels(serverId);
            return models;
        }

        [HttpPost("")]
        public async Task<ActionResult<ChatModelDto>> CreateChatModelAsync([FromBody] ChatModelDto model)
        {
            if (model == null)
                return BadRequest("Model data is required");
            var existingModel = await dbContext.Set<ChatModelDto>().FirstOrDefaultAsync(x => x.Name == model.Name);
            if (existingModel != null)
                return Conflict("A model with this name already exists");
            dbContext.Set<ChatModelDto>().Add(model);
            await dbContext.SaveChangesAsync();
            return CreatedAtAction(nameof(GetChatModelAsync), new { id = model.Id }, model);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<ChatModelDto>> UpdateChatModelAsync(Guid id, [FromBody] ChatModelDto model)
        {
            if (model == null)
                return BadRequest("Model data is required");
            var existingModel = await dbContext.Models.FirstOrDefaultAsync(x => x.Id == id);
            if (existingModel == null)
                return NotFound("Model not found");
            // Update properties - Only allow editing of metadata fields, not core model identifiers
            existingModel.Name = model.Name;
            existingModel.Description = model.Description;
            
            // Handle thinking capability - this is a custom capability we manage ourselves
            var currentCapabilities = existingModel.Capabilities?.ToList() ?? new List<string>();
            
            // Add or remove "thinking" capability based on EnableThinking toggle
            if (model.EnableThinking)
            {
                if (!currentCapabilities.Contains("thinking", StringComparer.OrdinalIgnoreCase))
                {
                    currentCapabilities.Add("thinking");
                }
            }
            else
            {
                currentCapabilities.RemoveAll(c => c.Equals("thinking", StringComparison.OrdinalIgnoreCase));
            }
            
            // Update capabilities with the modified list (preserving other capabilities)
            existingModel.Capabilities = currentCapabilities;
            
            // Only update Ollama-provided capabilities if they are provided and not empty
            // This prevents overwriting existing capabilities with empty data from the edit form
            if (model.Capabilities?.Strings?.Length > 0)
            {
                // Merge Ollama capabilities with our custom thinking capability
                var ollamaCapabilities = model.Capabilities.Strings.Where(c => !c.Equals("thinking", StringComparison.OrdinalIgnoreCase)).ToList();
                
                // Keep the thinking capability if EnableThinking is true
                if (model.EnableThinking && !ollamaCapabilities.Contains("thinking", StringComparer.OrdinalIgnoreCase))
                {
                    ollamaCapabilities.Add("thinking");
                }
                
                existingModel.Capabilities = ollamaCapabilities;
                existingModel.IsLLMModel = model.Capabilities.SupportsCompletion;
                existingModel.IsChatModel = model.Capabilities.SupportsCompletion;
            }
            
            existingModel.Family = model.Family;
            existingModel.ImageUrl = model.ImageUrl;
            existingModel.ParameterSize = model.ParameterSize;
            existingModel.QuantizationLevel = model.QuantizationLevel;
            existingModel.Size = model.Size;
            existingModel.Vram = model.Vram;
            existingModel.Type = model.Type;
            existingModel.Url = model.Url;
            existingModel.EnableThinking = model.EnableThinking;
            
            // DO NOT UPDATE these critical fields via edit form:
            // - Model: Core Ollama model identifier (e.g., "llama2:7b")
            // - IsLocal: Determined by sync with Ollama server
            // - IsRemote: Determined by sync with Ollama server  
            // - IsRunning: Real-time status from Ollama server
            // - ChatServerId: Server assignment should not change via edit
            // - CreatedAt: Historical timestamp
            
            // Only update these if capabilities were provided (to maintain consistency)
            if (model.Capabilities?.Strings?.Length > 0)
            {
                existingModel.IsChatModel = model.IsChatModel;
                existingModel.IsLLMModel = model.IsLLMModel;
            }

            dbContext.Models.Update(existingModel);
            await dbContext.SaveChangesAsync();
            return Ok(existingModel);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteChatModelAsync(Guid id)
        {
            try
            {
                logger.LogInformation("Attempting to delete chat model with ID: {ModelId}", id);
                
                // First check if the model exists to provide better error messages
                var existingModel = await dbContext.Models.FirstOrDefaultAsync(x => x.Id == id);
                if (existingModel == null)
                {
                    logger.LogWarning("Chat model with ID {ModelId} not found for deletion", id);
                    return NotFound($"Model with ID {id} not found");
                }
                
                logger.LogInformation("Deleting chat model: {ModelName} (ID: {ModelId})", existingModel.Name, id);
                
                var removed = await ollamaService.DeleteChatModelAsync(id, true);
                if (!removed)
                {
                    logger.LogError("Failed to delete chat model with ID: {ModelId}", id);
                    return BadRequest("Failed to delete model. The model may still be in use or there was an error communicating with the Ollama server.");
                }
                
                logger.LogInformation("Successfully deleted chat model: {ModelName} (ID: {ModelId})", existingModel.Name, id);
                return NoContent();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Exception occurred while deleting chat model with ID: {ModelId}", id);
                return StatusCode(500, "An error occurred while deleting the model. Please try again.");
            }
        }

        [HttpGet("capabilities")]
        public async Task<ActionResult<ModelCapabilities?>> GetModelCapabilitiesAsync([FromQuery] Guid serverId, [FromQuery] string model)
        {
            if (string.IsNullOrWhiteSpace(model))
                return BadRequest("Model name is required");

            var capabilities = await ollamaService.GetModelCapabilitiesAsync(serverId, model);
            if (capabilities == null)
                return NotFound();
            return Ok(capabilities);
        }

        [HttpGet("pull/{modelName}/stream")]
        public async IAsyncEnumerable<ProgressState> PullModelStreamAsync([FromQuery] Guid serverId, string modelName)
        {
            await foreach (var progress in ollamaService.PullModelAsync(serverId, modelName))
            {
                yield return progress;
            }
        }

        [HttpGet("pull/{modelName}")]
        public async Task PullModelAsync([FromQuery] Guid serverId, string modelName)
        {
            var connectionId = HttpContext.Connection.Id;
            var username = HttpContext.User.Identity?.Name ?? "Unknown";
            await taskQueue.EnqueueAsync(async token =>
            {
                logger.LogInformation("Pulling model {modelName} for user {username} with connection ID {connectionId}", modelName, username, connectionId);
                await hubContext.Clients.All.SendAsync("Notification", new ClientNotification
                {
                    ConnectionId = connectionId,
                    UserName = username ?? "System",
                    UserId = "System",
                    Message = $"Pulling model {modelName}...",
                    Type = ClientNotificationType.Info
                });
                try
                {
                    var progress = ollamaService.PullModelAsync(serverId, modelName);
                    await foreach (var p in progress)
                    { 
                        // "success"
                        if (p != null && p.Status == "success")
                        {
                            await hubContext.Clients.All.SendAsync("Notification", new ClientNotification
                            {
                                ConnectionId = connectionId,
                                UserName = username ?? "System",
                                UserId = "System",
                                Message = $"Model {modelName} pulled successfully.",
                                Type = ClientNotificationType.Success
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error pulling model {modelName}", modelName);
                    await hubContext.Clients.All.SendAsync("Notification", new ClientNotification
                    {
                        ConnectionId = connectionId,
                        UserName = "System",
                        UserId = "System",
                        Message = ex.ToString()
                    });
                    throw;
                }
            });

            await hubContext.Clients.All.SendAsync("Notification", "loops");

        }

        //RefreshCapabilitiesAsync
        [HttpGet("refresh-capabilities")]
        public async Task<IActionResult> RefreshCapabilitiesAsync([FromQuery] Guid serverId, [FromQuery] Guid[]? chatModelIds = null)
        {
            try
            {
                logger.LogInformation("RefreshCapabilitiesAsync called with serverId: {ServerId}, chatModelIds: {ChatModelIds}", 
                    serverId, chatModelIds?.Length > 0 ? string.Join(",", chatModelIds) : "null");
                
                await ollamaService.RefreshCapabilitiesAsync(serverId, chatModelIds);
                return Ok(new { message = "Capabilities refreshed successfully." });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error refreshing capabilities");
                return StatusCode(500, new { message = "Error refreshing capabilities", error = ex.Message });
            }
        }


    }
}
