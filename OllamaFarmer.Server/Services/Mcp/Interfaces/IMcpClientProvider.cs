using ModelContextProtocol.Client;
using OllamaFarmer.Data.Entities;

namespace OllamaFarmer.Server.Services.Mcp.Interfaces
{
    public interface IMcpClientProvider
    {
        Task<IMcpClient> GetMcpClientAsync(Uri mcpApiUri);
        Task<IList<McpClientTool>> ListApiToolsAsync(Uri mcpApiUri);
    }
}
