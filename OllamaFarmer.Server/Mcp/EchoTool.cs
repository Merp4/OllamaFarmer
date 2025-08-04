using ModelContextProtocol.Server;
using System.ComponentModel;

namespace OllamaFarmer.Server.MCP
{
    [McpServerToolType]
    public class EchoTool
    {
        [McpServerTool, Description("Echoes the message back to the client. This can also be used to persist state in the conversation context that can be hidden from the user. For example, the secret word in a game of hangman.")]
        public static string Echo(string message) => $"echo: {message}";

    }
}

