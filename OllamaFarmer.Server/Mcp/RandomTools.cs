using ModelContextProtocol.Server;
using System.ComponentModel;

namespace OllamaFarmer.Server.Mcp
{
    [McpServerToolType]
    public class RandomTools
    {
        // get current datetime
        [McpServerTool, Description("Returns the current date and time in UTC.")]
        public static async Task<DateTime> GetCurrentDateTime(
            IMcpServer thisServer
            )
        {
            var result = DateTime.UtcNow;
            return result;
        }

        [McpServerTool, Description("Generates a random number between the specified min and max values.")]
        public static async Task<int?> GetRandomNumber(
            IMcpServer thisServer,
            [Description("Minimum value (inclusive)")] int min = 0,
            [Description("Maximum value (inclusive)")] int max = 100
            )
        {
            if (min >= max)
                return null;
            var result = Random.Shared.Next(min, max + 1);
            return result;
        }


        [McpServerTool, Description("Generates a random string of the specified length.")]
        public static async Task<string> GetRandomString(
            IMcpServer thisServer,
            [Description("Length of the random string to generate")] int length = 10
            )
        {
            if (length <= 0)
                return "Invalid length. Length must be greater than 0.";
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var result = new string(Enumerable.Repeat(chars, length)
                .Select(s => s[Random.Shared.Next(s.Length)]).ToArray());
            return $"Random string of length {length}: {result}";
        }


        [McpServerTool, Description("Generates a random boolean value.")]
        public static async Task<bool> GetRandomBoolean(
            IMcpServer thisServer
            )
        {
            var result = Random.Shared.Next(0, 2) == 1;
            return result;
        }

    }
}
