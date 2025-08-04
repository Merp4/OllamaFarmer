using Microsoft.Extensions.AI;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace OllamaFarmer.Server.MCP
{
    [McpServerToolType]
    public class LogTool
    {
        [McpServerTool, Description("Logs a message to the server logs.")]
        public static string LogToServer(
            IMcpServer thisServer,
            //IServiceScopeFactory serviceScopeFactory,
            [Description("message")] string? message)
        {
            try
            {
                message ??= string.Empty;
                thisServer.Services?
                    .CreateScope().ServiceProvider
                    .GetRequiredService<ILogger<LogTool>>()
                    .LogInformation("LogToServer: '{Message}'", message);
            }
            catch (Exception ex)
            {
                // Log the error to the server logs
                thisServer.Services?
                    .CreateScope().ServiceProvider
                    .GetRequiredService<ILogger<LogTool>>()
                    .LogError(ex, "Failed to log message: {Message}", message);
                return "Error logging message: " + ex.Message;
            }

            return "Message logged successfully: " + message;
        }



        //[McpServerTool]
        //public static DataContent GetMyImage()
        //{
        //    byte[] bytes = File.ReadAllBytes("path/to/my/image.png");
        //    return new DataContent(bytes, "image/png");
        //}
    }
}



/*
You are a calculation formula assistant, this is your sole concern. You will be provided with a set of fields, and a user prompt.
You will use the provided fields as the variables in the formula as appropriate. You will only use simple mathematic operations.
Prompts will be provided to you in the format <fields> ... comma separated fields ... </fields> <prompt> ... user prompt ... </prompt>.
Your responses will only be in the format <calc> ... the proposed formula/calculation ... </calc> <msg> ... a short explanation ... </msg>.
*/
