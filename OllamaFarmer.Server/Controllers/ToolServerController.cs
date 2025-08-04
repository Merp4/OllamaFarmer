using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.SignalR;
using OllamaFarmer.Server.Data.Entities;
using OllamaFarmer.Server.Services.Background.Interfaces;
using OllamaFarmer.Server.Services.Mcp;
using OllamaFarmer.Server.Services.Mcp.Interfaces;
using OllamaFarmer.Server.Services.SignalR;

namespace OllamaFarmer.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ToolServerController(ILogger<ToolServerController> logger,
        IBackgroundTaskQueue backgroundTaskQueue,
        IMcpServerRepository toolServerRepository,
        IHubContext<NotificationHub> hubContext,
        IMcpToolService mcpToolService)
        : ControllerBase
    {
        // MCP Server management controller

        [HttpGet("{id}")]
        public async Task<ActionResult<McpServer>> GetMcpServerAsync(Guid id)
        {
            var server = await toolServerRepository.GetMcpServerByIdAsync(id);
            if (server == null)
                return NotFound();
            return Ok(server);
        }

        [HttpGet("all")]
        public async Task<ActionResult<List<McpServer>>> GetAllMcpServersAsync()
        {
            var servers = await toolServerRepository.GetAllMcpServersAsync();
            return Ok(servers);
        }

        [HttpGet("{serverId}/sync")]
        public async Task<ActionResult> SyncMcpServerToolsAsync(Guid serverId)
        {
            try
            {
                // Update or create tools in the database
                var tools = await mcpToolService.SyncToolsFromServerAsync(serverId);

                return Ok();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error synchronizing MCP server tools for server ID: {ServerId}", serverId);
                return StatusCode(500, "An error occurred while synchronizing the MCP server tools");
            }
        }

        [HttpGet("sync")]
        public async Task<ActionResult> SyncAllMcpServersAsync()
        {
            // Fire off a background task to synchronize all MCP servers and their tools

            var connectionId = HttpContext.Connection.Id;
            var username = HttpContext.User.Identity.Name;

            await backgroundTaskQueue.EnqueueAsync(async token =>
            {
                try
                {
                    await hubContext.Clients.All.SendAsync("Notification", new ClientNotification
                    {
                        ConnectionId = connectionId,
                        UserName = username ?? "System",
                        UserId = "System",
                        Message = $"Starting synchronization of all MCP servers...",
                        Type = ClientNotificationType.Info
                    });

                    var servers = await toolServerRepository.GetAllMcpServersAsync();
                    foreach (var server in servers)
                    {
                        var tools = await mcpToolService.SyncToolsFromServerAsync(server.Id);
                        if (tools != null)
                            logger.LogInformation("Synchronized {ToolCount} tools for MCP server with ID: {ServerId}", tools.Count, server.Id);
                        
                    }
                    logger.LogInformation("Synchronized all MCP servers and their tools");

                    await hubContext.Clients.All.SendAsync("Notification", new ClientNotification
                    {
                        ConnectionId = connectionId,
                        UserName = username ?? "System",
                        UserId = "System",
                        Message = $"Synchronization of all MCP servers completed successfully.",
                        Type = ClientNotificationType.Success
                    });
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error synchronizing all MCP servers");

                    await hubContext.Clients.All.SendAsync("Notification", new ClientNotification
                    {
                        ConnectionId = connectionId,
                        UserName = username ?? "System",
                        UserId = "System",
                        Message = $"Error synchronizing all MCP servers: {ex.Message}",
                        Type = ClientNotificationType.Error
                    });
                }
            });

            logger.LogInformation("Started background synchronization of all MCP servers");
            return Accepted("Synchronization started. You will be notified when it completes.");
        }

        // CRUD Operations

        [HttpPost]
        public async Task<ActionResult<McpServer>> CreateMcpServerAsync([FromBody] McpServer mcpServer)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var createdServer = await toolServerRepository.CreateMcpServerAsync(mcpServer);
                logger.LogInformation("Created MCP server with ID: {Id}", createdServer.Id);
                return CreatedAtAction(nameof(GetMcpServerAsync), new { id = createdServer.Id }, createdServer);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating MCP server");
                return StatusCode(500, "An error occurred while creating the MCP server");
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<McpServer>> UpdateMcpServerAsync(Guid id, [FromBody] McpServer mcpServer)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (id != mcpServer.Id)
                return BadRequest("ID in URL does not match ID in request body");

            try
            {
                var updatedServer = await toolServerRepository.UpdateMcpServerAsync(mcpServer);
                if (updatedServer == null)
                    return NotFound();

                logger.LogInformation("Updated MCP server with ID: {Id}", updatedServer.Id);
                return Ok(updatedServer);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error updating MCP server with ID: {Id}", id);
                return StatusCode(500, "An error occurred while updating the MCP server");
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteMcpServerAsync(Guid id)
        {
            try
            {
                var deleted = await toolServerRepository.DeleteMcpServerAsync(id);
                if (!deleted)
                    return NotFound();

                logger.LogInformation("Deleted MCP server with ID: {Id}", id);
                return NoContent();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deleting MCP server with ID: {Id}", id);
                return StatusCode(500, "An error occurred while deleting the MCP server");
            }
        }

        // Tool management endpoints

        [HttpGet("{serverId}/tools")]
        public async Task<ActionResult<List<McpTool>>> GetMcpToolsAsync(Guid serverId)
        {
            var tools = await toolServerRepository.GetAllMcpToolsAsync(serverId);
            return Ok(tools);
        }

        [HttpGet("{serverId}/tools/{toolId}")]
        public async Task<ActionResult<McpTool>> GetMcpToolAsync(Guid serverId, Guid toolId)
        {
            var tool = await toolServerRepository.GetMcpToolByIdAsync(toolId);
            if (tool == null || tool.McpServerId != serverId)
                return NotFound();
            return Ok(tool);
        }

        [HttpPost("{serverId}/tools")]
        public async Task<ActionResult<McpTool>> CreateMcpToolAsync(Guid serverId, [FromBody] McpTool mcpTool)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Ensure the tool belongs to the specified server
            mcpTool.McpServerId = serverId;

            try
            {
                var createdTool = await toolServerRepository.CreateMcpToolAsync(mcpTool);
                logger.LogInformation("Created MCP tool with ID: {Id} for server: {ServerId}", createdTool.Id, serverId);
                return CreatedAtAction(nameof(GetMcpToolAsync), new { serverId, toolId = createdTool.Id }, createdTool);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating MCP tool for server: {ServerId}", serverId);
                return StatusCode(500, "An error occurred while creating the MCP tool");
            }
        }

        [HttpPut("{serverId}/tools/{toolId}")]
        public async Task<ActionResult<McpTool>> UpdateMcpToolAsync(Guid serverId, Guid toolId, [FromBody] McpTool mcpTool)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (toolId != mcpTool.Id)
                return BadRequest("Tool ID in URL does not match ID in request body");

            // Ensure the tool belongs to the specified server
            mcpTool.McpServerId = serverId;

            try
            {
                var updatedTool = await toolServerRepository.UpdateMcpToolAsync(mcpTool);
                if (updatedTool == null)
                    return NotFound();

                logger.LogInformation("Updated MCP tool with ID: {Id} for server: {ServerId}", toolId, serverId);
                return Ok(updatedTool);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error updating MCP tool with ID: {Id} for server: {ServerId}", toolId, serverId);
                return StatusCode(500, "An error occurred while updating the MCP tool");
            }
        }

        [HttpDelete("{serverId}/tools/{toolId}")]
        public async Task<ActionResult> DeleteMcpToolAsync(Guid serverId, Guid toolId)
        {
            try
            {
                // Verify the tool exists and belongs to the specified server
                var tool = await toolServerRepository.GetMcpToolByIdAsync(toolId);
                if (tool == null || tool.McpServerId != serverId)
                    return NotFound();

                var deleted = await toolServerRepository.DeleteMcpToolAsync(toolId);
                if (!deleted)
                    return NotFound();

                logger.LogInformation("Deleted MCP tool with ID: {Id} for server: {ServerId}", toolId, serverId);
                return NoContent();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deleting MCP tool with ID: {Id} for server: {ServerId}", toolId, serverId);
                return StatusCode(500, "An error occurred while deleting the MCP tool");
            }
        }

        // Additional utility endpoints

        [HttpGet("tools/by-ids")]
        public async Task<ActionResult<List<McpTool>>> GetToolsByIdsAsync([FromQuery] Guid[] toolIds)
        {
            if (toolIds == null || toolIds.Length == 0)
                return BadRequest("Tool IDs are required");

            var tools = await toolServerRepository.GetToolsByIdsAsync(toolIds);
            return Ok(tools);
        }

        [HttpGet("by-tool-ids")]
        public async Task<ActionResult<List<McpServer>>> GetServersByToolIdsAsync([FromQuery] Guid[] toolIds)
        {
            if (toolIds == null || toolIds.Length == 0)
                return BadRequest("Tool IDs are required");

            var servers = await toolServerRepository.GetMcpServerByToolIds(toolIds);
            return Ok(servers);
        }
    }
}
