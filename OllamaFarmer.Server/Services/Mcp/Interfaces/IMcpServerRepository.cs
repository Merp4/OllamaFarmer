using OllamaFarmer.Server.Data.Entities;

namespace OllamaFarmer.Server.Services.Mcp.Interfaces
{
    public interface IMcpServerRepository
    {
        Task<McpServer> CreateMcpServerAsync(McpServer mcpServer);
        Task<McpTool> CreateMcpToolAsync(McpTool mcpTool);
        Task<bool> DeleteMcpServerAsync(Guid id);
        Task<bool> DeleteMcpToolAsync(Guid id);
        Task<List<McpServer>> GetAllMcpServersAsync();
        Task<List<McpTool>> GetAllMcpToolsAsync(Guid mcpServerId);
        Task<McpServer?> GetMcpServerByIdAsync(Guid id);
        Task<McpTool?> GetMcpToolByIdAsync(Guid id);
        Task<McpTool?> GetMcpToolByNameAsync(Guid mcpServerId, string name);
        Task<McpServer?> UpdateMcpServerAsync(McpServer mcpServer);
        Task<McpTool?> UpdateMcpToolAsync(McpTool mcpTool);
        Task<List<McpServer>> GetMcpServerByToolIds(Guid[] toolIds);
        Task<List<McpTool>> GetToolsByIdsAsync(Guid[] toolsIds);
        Task<IEnumerable<McpTool>> CreateMcpToolsAsync(IEnumerable<McpTool> mcpTools);
        Task<bool> DeleteMcpToolsAsync(IEnumerable<Guid> ids);
    }
}
