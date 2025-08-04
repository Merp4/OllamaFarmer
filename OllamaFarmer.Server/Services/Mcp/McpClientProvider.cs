using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Memory;
using ModelContextProtocol.Client;
using ModelContextProtocol.AspNetCore;
using ModelContextProtocol.Protocol;
using OllamaFarmer.Server.Services.Mcp.Interfaces;

namespace OllamaFarmer.Server.Services.Mcp
{
    public class McpClientProvider : IMcpClientProvider
    {

        public async Task<IMcpClient> GetMcpClientAsync(Uri mcpApiUri)
        {
            IClientTransport transport = new SseClientTransport(new SseClientTransportOptions
            {
                Endpoint = mcpApiUri,
            });
            
            return await McpClientFactory.CreateAsync(transport);
        }

        public async Task<IList<McpClientTool>> ListApiToolsAsync(Uri mcpApiUri)
        {
            return await FetchApiToolsAsync(mcpApiUri);
        }

        private async Task<IList<McpClientTool>> FetchApiToolsAsync(Uri mcpApiUri)
        {
            var client = await GetMcpClientAsync(mcpApiUri);
            var tools = await client.ListToolsAsync();
            return tools;
        }
    }
}
