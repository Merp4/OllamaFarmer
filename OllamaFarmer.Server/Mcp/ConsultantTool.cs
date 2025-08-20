using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;
using OllamaFarmer.Server.MCP;
using OllamaFarmer.Server.Services.Interfaces;
using System.ComponentModel;
using System.Text;
using System.Text.RegularExpressions;

namespace OllamaFarmer.Server.Mcp
{

    [McpServerToolType]
    public class ConsultantTool
    {
        [McpServerTool, Description("Leverage the knowledge of configured consultants. You can specify either the ConsultantId (preferred) or the consultant Name.")]
        public static async Task<string> QueryConsultant(
            IMcpServer thisServer,
            [Description("Consultant identifier. Accepts ConsultantId or Name (case-insensitive).")]
            string consultantId,
            [Description("The context or question to ask the consultant about. This must not be null or empty.")]
            string enquiryContext
        )
        {
            if (string.IsNullOrWhiteSpace(consultantId))
            {
                return "Null or empty consultant identifier provided. Please specify a valid ConsultantId or Name.";
            }
            if (string.IsNullOrWhiteSpace(enquiryContext))
            {
                return $"{nameof(enquiryContext)} cannot be null or empty.";
            }

            using var ctx = new ToolContext(nameof(ConsultantTool), thisServer, thisServer.Services!);

            var db = ctx.AppDbContext;

            // 1) Attempt exact ConsultantId match
            var consultant = await db.Consultants.FirstOrDefaultAsync(c => c.ConsultantId == consultantId);

            // 2) Fallback: attempt Name match (case-insensitive exact)
            if (consultant == null)
            {
                // Try case-sensitive exact first (SQL-translatable)
                consultant = await db.Consultants.FirstOrDefaultAsync(c => c.Name == consultantId);

                if (consultant == null)
                {
                    // Case-insensitive in-memory fallback for providers/collations
                    var byName = await db.Consultants.Where(c => c.Name != null).ToListAsync();
                    consultant = byName.FirstOrDefault(c => string.Equals(c.Name, consultantId, StringComparison.OrdinalIgnoreCase));
                }
            }

            if (consultant == null)
            {
                return "Consultant not found. Please specify a valid ConsultantId or Name.";
            }

            // Resolve model
            var chatModel = await db.Models.FirstOrDefaultAsync(m => m.Id == consultant.ChatModelId);
            if (chatModel == null)
            {
                ctx.Logger.LogError("ChatModel not found for consultant {ConsultantKey}", consultant.ConsultantId);
                return "Consultation failed. Model mapping is invalid.";
            }

            ctx.Logger.LogInformation("Consulting with model {Model} for enquiry: {EnquiryContext}", chatModel.Model, enquiryContext);

            var appChatService = ctx.GetService<IAppChatService>();
            var response = await appChatService.GenerateResponseAsync(
                consultant.ChatServerId,
                chatModel.Model ?? string.Empty,
                consultant.SystemMessage ?? string.Empty,
                enquiryContext,
                consultant.Temperature,
                consultant.TopP,
                consultant.FrequencyPenalty,
                consultant.PresencePenalty);

            if (string.IsNullOrWhiteSpace(response))
            {
                ctx.Logger.LogError("Consultation failed for enquiry: {EnquiryContext}", enquiryContext);
                return "Consultation failed. Please try again later.";
            }
            ctx.Logger.LogInformation("Consultation successful for enquiry: {EnquiryContext}", enquiryContext);

            // Remove thoughts - <think></think> tags 
            return Regex.Replace(response, @"<think>(.*?)</think>", string.Empty, RegexOptions.IgnoreCase).Trim();
        }

        [McpServerTool, Description("Retrieves a list of available consultants in markdown (shows Name and ConsultantId).")]
        public static string GetAvailableConsultantsAsMarkdown(
            IMcpServer thisServer
            )
        {
            using var ctx = new ToolContext(nameof(ConsultantTool), thisServer, thisServer.Services!);
            var db = ctx.AppDbContext;
            var consultants = db.Consultants.OrderBy(c => c.Topic).ThenBy(c => c.Name).ToList();
            if (consultants.Count == 0)
            {
                return "No consultants available.";
            }

            // Resolve model names in one query
            var modelIds = consultants.Select(c => c.ChatModelId).Distinct().ToList();
            var modelMap = db.Models
                .Where(m => modelIds.Contains(m.Id))
                .Select(m => new { m.Id, m.Model })
                .ToList()
                .ToDictionary(x => x.Id, x => x.Model ?? "unknown");

            var sb = new StringBuilder();
            sb.Append("Available Consultants:\n");
            foreach (var consultant in consultants)
            {
                var modelName = modelMap.TryGetValue(consultant.ChatModelId, out var v) ? v : "unknown";
                sb.Append($"- {consultant.Name} (ConsultantId: `{consultant.ConsultantId}`) topic: {consultant.Topic} (Model: {modelName}, Expertise Level: {consultant.ExpertiseLevel})\n");
            }
            return sb.ToString();
        }

        [McpServerTool, Description("Retrieves a JSON list of consultants (Name, ConsultantId, Topic, ExpertiseLevel).")]
        public static IEnumerable<object> GetAvailableConsultants(
            IMcpServer thisServer
            )
        {
            using var ctx = new ToolContext(nameof(ConsultantTool), thisServer, thisServer.Services!);
            var db = ctx.AppDbContext;
            return db.Consultants
                .OrderBy(c => c.Topic).ThenBy(c => c.Name)
                .Select(c => new { c.Name, c.ConsultantId, c.ExpertiseLevel, c.Topic })
                .ToList();
        }

    }
}
