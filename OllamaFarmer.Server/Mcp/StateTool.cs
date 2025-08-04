using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;
using OllamaFarmer.Server.Data;
using OllamaFarmer.Server.Mcp;
using System.ComponentModel;
using System.Text;

namespace OllamaFarmer.Server.MCP
{
    [McpServerToolType()]
    public class StateTool
    {

        [McpServerTool, Description("Stores data for future retrieval. So AIs can reliably persist state or context.")]
        public static async Task<string> SetState(
            IMcpServer thisServer,
            [Description("A string key for storing a state")]
            string stateKey,
            [Description("A string containing data representing a state")]
            string stateValue
        )
        {
            using var ctx = new ToolContext(nameof(StateTool), thisServer, thisServer.Services!);

            if (string.IsNullOrWhiteSpace(stateKey) || string.IsNullOrWhiteSpace(stateValue))
            {
                ctx.Logger.LogError("State key or value is null or empty.");
                return "State key or value is null or empty.";
            }

            ctx.AppDbContext.States.Add(new KvpState
            {
                Key = stateKey,
                Value = stateValue
            });
            try
            {
                await ctx.AppDbContext.SaveChangesAsync();
                ctx.Logger.LogInformation("State stored successfully: {Key}", stateKey);
                return $"State stored successfully: {stateKey}";
            }
            catch (Exception ex)
            {
                ctx.Logger.LogError(ex, "Error storing state: {Key}", stateKey);
                return $"Error storing state: {ex.Message}";
            }

        }



        [McpServerTool, Description("Retrieves data stored by SetState. So AIs can reliably persist state or context.")]
        public static async Task<string> GetState(
            IMcpServer thisServer,
            [Description("A string key for retrieving a state")]
            string stateKey
        )
        {
            using var ctx = new ToolContext(nameof(StateTool), thisServer, thisServer.Services!);
            if (string.IsNullOrWhiteSpace(stateKey))
            {
                ctx.Logger.LogError("State key is null or empty.");
                return "State key is null or empty.";
            }
            var state = await ctx.AppDbContext.States
                .FirstOrDefaultAsync(s => s.Key == stateKey);
            if (state == null)
            {
                ctx.Logger.LogInformation("State not found: {Key}", stateKey);
                return $"State not found: {stateKey}";
            }
            ctx.Logger.LogInformation("State retrieved successfully: {Key}", stateKey);
            return state.Value;
        }

    }
}

