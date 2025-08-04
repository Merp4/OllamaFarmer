using Microsoft.Extensions.Caching.Memory;
using ModelContextProtocol.Client;
using OllamaFarmer.Server.Data.Entities;
using OllamaFarmer.Server.Services.Mcp.Interfaces;

namespace OllamaFarmer.Server.Services.Mcp
{

    public interface IMcpToolService
    {
        //Task<List<McpTool>> ListToolsFromServerAsync(Guid serverId);
        Task<List<McpTool>> ListToolsFromDbAsync(Guid? serverId);
        Task<List<McpTool>> ListToolsCachedAsync(Guid? serverId);
        Task<List<McpClientTool>> GetClientToolsAsync(Guid[]? toolIds);
        Task<IMcpClient> GetClientForServer(Guid serverId);
        Task<List<McpTool>> SyncToolsFromServerAsync(Guid serverId);
    }

    public class McpToolService(ILogger<McpToolService> logger, IMcpClientProvider mcpService, IMcpServerRepository mcpServerRepository, IMemoryCache cache)
        : IMcpToolService
    {
        protected async Task<List<McpTool>> ListToolsFromServerAsync(Guid serverId)
        {
            var server = await mcpServerRepository.GetMcpServerByIdAsync(serverId);
            if (server == null)
                throw new ArgumentException($"MCP Server with ID {serverId} not found.");
            var tools = await mcpService.ListApiToolsAsync(server.Uri);
            return tools.Select(tool => new McpTool
            {
                Name = tool.Name,
                Description = tool.Description,
                McpServerId = serverId,
            }).ToList();
        }
        public async Task<List<McpTool>> SyncToolsFromServerAsync(Guid serverId)
        {
            var server = await mcpServerRepository.GetMcpServerByIdAsync(serverId);
            if (server == null)
                throw new ArgumentException($"MCP Server with ID {serverId} not found.");
            var remoteTools = await mcpService.ListApiToolsAsync(server.Uri);
            var existingTools = await mcpServerRepository.GetAllMcpToolsAsync(serverId);

            // match on function name
            var toolsToAdd = remoteTools
                .Where(remoteTool => !existingTools.Any(existingTool => existingTool.Name == remoteTool.Name))
                .Select(tool => new McpTool
                {
                    Name = tool.Name,
                    Description = tool.Description,
                    McpServerId = serverId,
                }).ToList();
            var toolsToDelete = existingTools
                .Where(existingTool => !remoteTools.Any(remoteTool => remoteTool.Name == existingTool.Name))
                .ToList();
            var toolsToUpdate = existingTools
                .Where(existingTool => remoteTools.Any(remoteTool => remoteTool.Name == existingTool.Name &&
                                                                     remoteTool.Description != existingTool.Description))
                .Select(existingTool =>
                {
                    var remoteTool = remoteTools.First(t => t.Name == existingTool.Name);
                    existingTool.Description = remoteTool.Description;
                    return existingTool;
                }).ToList();

            // add new tools
            if (toolsToAdd.Count > 0)
                await mcpServerRepository.CreateMcpToolsAsync(toolsToAdd);

            // delete old tools
            if (toolsToDelete.Count > 0)
                await mcpServerRepository.DeleteMcpToolsAsync(toolsToDelete.Select(t => t.Id));

            // update existing tools
            if (toolsToUpdate.Count > 0)
                foreach (var tool in toolsToUpdate)
                    await mcpServerRepository.UpdateMcpToolAsync(tool);

            // return all tools after sync
            return await mcpServerRepository.GetAllMcpToolsAsync(serverId);
        }


        public async Task<List<McpTool>> ListToolsFromDbAsync(Guid? serverId)
        {
            // get from db, if serverId is null or empty GUID, get all servers
            if (serverId.HasValue && serverId.Value != Guid.Empty)
            {
                var server = await mcpServerRepository.GetMcpServerByIdAsync(serverId.Value);
                if (server == null)
                    throw new ArgumentException($"MCP Server with ID {serverId} not found.");
                return server.McpTools.ToList();
            }
            else
            {
                var servers = await mcpServerRepository.GetAllMcpServersAsync();
                return servers.SelectMany(s => s.McpTools).ToList();
            }
        }

        public async Task<List<McpTool>> ListToolsCachedAsync(Guid? serverId)
        {
            var result = await cache.GetOrCreateAsync($"McpTools_{serverId}", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
                return await ListToolsFromDbAsync(serverId);
            });
            // Ensure non-null return to satisfy nullable analysis
            return result ?? new List<McpTool>();
        }

        public async Task<List<McpClientTool>> GetClientToolsAsync(Guid[]? toolIds)
        {
            if (toolIds == null || toolIds.Length == 0)
            {
                return new List<McpClientTool>();
            }

            // we need to find all the tools in the DB that match the toolIds
            // we need all the relevant tools from the MCP service from each server - ?? can we not just construct the tools ourselves...?
            var tools = await mcpServerRepository.GetToolsByIdsAsync(toolIds);
            var toolServers = await mcpServerRepository.GetMcpServerByToolIds(toolIds);
            if (tools == null
                || tools.Count == 0
                || toolServers == null
                || toolServers.Count == 0)
            {
                return new List<McpClientTool>();
            }
            var clientTools = new List<McpClientTool>();
            foreach (var server in toolServers)
            {
                var dbTools = tools.Where(t => t.McpServerId == server.Id).ToList();
                var apiTools = await mcpService.ListApiToolsAsync(server.Uri);

                var matchingTools = apiTools.Where(t => dbTools.Any(dbTool => dbTool.Name == t.Name)).ToList();
                if (matchingTools.Count > 0)
                {
                    clientTools.AddRange(matchingTools);
                }
            }

            if (clientTools.Count != toolIds.Length)
            {
                // we have not found all the tools, for now log a warning
                logger.LogWarning("Not all tools were found in the MCP service. Expected {ExpectedCount}, Found {FoundCount}", toolIds.Length, clientTools.Count);
            }

            return clientTools;
        }

        public async Task<IMcpClient> GetClientForServer(Guid serverId)
        {
            var server = await mcpServerRepository.GetMcpServerByIdAsync(serverId);
            return await mcpService.GetMcpClientAsync(server.Uri ?? throw new ArgumentException($"MCP Server with ID {serverId} does not have a valid URI."))
                ?? throw new InvalidOperationException($"Failed to create MCP client for server with ID {serverId}.");
        }
    }
}
