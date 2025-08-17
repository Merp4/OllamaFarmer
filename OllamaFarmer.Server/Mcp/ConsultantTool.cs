using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;
using OllamaFarmer.Server.MCP;
using OllamaFarmer.Server.Services.Interfaces;
using OllamaFarmer.SomeOtherNamespaceforlater;
using System;
using System.ComponentModel;
using System.Text;
using System.Text.RegularExpressions;

namespace OllamaFarmer.SomeOtherNamespaceforlater
{
    public enum ConsultantExpertiseLevel
    {
        Beginner,
        Intermediate,
        Advanced,
        Expert
    }

    public class Consultant
    {
        // a consultant essentially represents an AI model or a specific configuration for consulting another AI model.
        // this model may be on a remote server or locally hosted relative to the server.
        public required string Topic { get; set; } 

        public Guid ServerId { get; set; }
        public required string Model { get; set; }
        public string? SystemMessage { get; set; }

        // Indicates the expertise or level of depth of the consultant.
        public ConsultantExpertiseLevel ExpertiseLevel { get; set; }

        public float Temperature { get; set; } = 0.7f; // Default temperature
        public float TopP { get; set; } = 1.0f; // Default top_p
        public float FrequencyPenalty { get; set; } = 0.0f; // Default frequency penalty
        public float PresencePenalty { get; set; } = 0.0f; // Default presence penalty
        public string ConsultantId { get; set; }
    }

}

namespace OllamaFarmer.Server.Mcp
{

    [McpServerToolType]
    public class ConsultantTool
    {
        private static List<Consultant> consultants = new List<Consultant>
        {
            new Consultant() {
                ServerId = Guid.Parse("60a59097-2ec3-4bd1-ba7f-28f5d74af114"),
                ConsultantId = "CON-01-01", 
                Topic = "General",
                Model = "qwen3:0.6b", ExpertiseLevel = ConsultantExpertiseLevel.Beginner,
                SystemMessage = "You are a general knowledge consultant tasked with providing a concise, informative, and accurate, reply to an inquiry. You will not respond with any questions, you will provide a short statement in response.",
                Temperature = 0.5f, TopP = 0.9f, FrequencyPenalty = 0.1f, PresencePenalty = 0.0f },
            new Consultant() {
                ServerId = Guid.Parse("60a59097-2ec3-4bd1-ba7f-28f5d74af114"),
                ConsultantId = "CON-01-02",
                Topic = "General",
                Model = "gemma3:1b", ExpertiseLevel = ConsultantExpertiseLevel.Intermediate,
                SystemMessage = "You are a general knowledge consultant tasked with providing a concise, informative, and accurate, reply to an inquiry. You will not respond with any questions, you will provide a short statement in response.",
                Temperature = 0.5f, TopP = 0.9f, FrequencyPenalty = 0.1f, PresencePenalty = 0.0f },
            new Consultant() {
                ServerId = Guid.Parse("60a59097-2ec3-4bd1-ba7f-28f5d74af114"),
                ConsultantId = "CON-01-03",
                Topic = "General",
                Model = "qwen3:4b", ExpertiseLevel = ConsultantExpertiseLevel.Advanced,
                SystemMessage = "You are a general knowledge consultant tasked with providing a concise, informative, and accurate, reply to an inquiry. You will not respond with any questions, you will provide a short statement in response.",
                Temperature = 0.5f, TopP = 0.9f, FrequencyPenalty = 0.1f, PresencePenalty = 0.0f },
            new Consultant() {
                ServerId = Guid.Parse("60a59097-2ec3-4bd1-ba7f-28f5d74af114"),
                ConsultantId = "CON-02-01",
                Topic = "Code",
                Model = "mistral:latest", ExpertiseLevel = ConsultantExpertiseLevel.Expert,
                SystemMessage = "You are an expert coding consultant tasked with providing a concise, deeply informative, and accurate, reply to an inquiry. You will not respond with any questions, you will provide a short statement in response.",
                Temperature = 0.5f, TopP = 0.9f, FrequencyPenalty = 0.1f, PresencePenalty = 0.0f },
            new Consultant() {
                ServerId = Guid.Parse("60a59097-2ec3-4bd1-ba7f-28f5d74af114"),
                ConsultantId = "CON-03-01",
                Topic = "Maths",
                Model = "phi4-mini:3.8b", ExpertiseLevel = ConsultantExpertiseLevel.Expert,
                SystemMessage = "You are an expert mathematics consultant tasked with providing a concise, deeply informative, and accurate, reply to an inquiry. You will not respond with any questions, you will provide a short statement in response.",
                Temperature = 0.5f, TopP = 0.9f, FrequencyPenalty = 0.1f, PresencePenalty = 0.0f },
        };


        [McpServerTool, Description("This tool allows you to leverage the knowledge, expertise, and perspectives of various consultants. You can specify which consultant by their ConsultantId.")]
        public static async Task<string> QueryConsultant(
            IMcpServer thisServer,
            [Description("The ConsultantId of the consultant to use for the enquiry. This must not be null or empty.")]
            string consultantId,
            [Description("The context or question to ask the consultant about. This must not be null or empty.")]
            string enquiryContext
        )
        {
            if (consultantId == null || consultantId.Trim().Length == 0)
            {
                return "Null or empty ConsultantId provided. Please specify a valid ConsultantId.";
            }
            var consultant = consultants.FirstOrDefault(c => c.ConsultantId.Equals(consultantId, StringComparison.OrdinalIgnoreCase));
            if (consultant == null)
            {
                return "Consultant not found. Please specify a valid ConsultantId.";
            }
            if (string.IsNullOrWhiteSpace(enquiryContext))
            {
                return $"{nameof(enquiryContext)} cannot be null or empty.";
            }

            using var ctx = new ToolContext(nameof(ConsultantTool), thisServer, thisServer.Services!);
            ctx.Logger.LogInformation("Consulting with {ConsultantName} for enquiry: {EnquiryContext}", consultant.Model, enquiryContext);

            var appChatService = ctx.GetService<IAppChatService>();
            var response = await appChatService.GenerateResponseAsync(consultant.ServerId, consultant.Model, consultant.SystemMessage, enquiryContext,
               consultant.Temperature,
                consultant.TopP,
                 consultant.FrequencyPenalty,
                 consultant.PresencePenalty);

            if (response == null)
            {
                ctx.Logger.LogError("Consultation failed for enquiry: {EnquiryContext}", enquiryContext);
                return "Consultation failed. Please try again later.";
            }
            ctx.Logger.LogInformation("Consultation successful for enquiry: {EnquiryContext}", enquiryContext);

            // Remove thoughts - <think></think> tags 
            return Regex.Replace(response, @"<think>(.*?)</think>", string.Empty, RegexOptions.IgnoreCase).Trim();
        }

        [McpServerTool, Description("Retrieves a list of available consultants in markdown format including their ConsultantId. This can be used to see which consultants are available for consultation with their corresponding ConsultantId. Consultants can provide additional advice, different perspectives on a topic, or advanced/alternative knowledge.")]
        public static string GetAvailableConsultantsAsMarkdown(
            IMcpServer thisServer
            )
        {
            using var ctx = new ToolContext(nameof(ConsultantTool), thisServer, thisServer.Services!);
            if (consultants.Count == 0)
            {
                return "No consultants available.";
            }
            var sb = new StringBuilder();
            sb.Append("Available Consultants:\n");
            foreach (var consultant in consultants)
            {
                sb.Append($"- ConsultantId: `{consultant.ConsultantId}` for knowledge topic: {consultant.Topic} (Model: {consultant.Model}, Expertise Level: {consultant.ExpertiseLevel.ToString()})\n");
            }
            return sb.ToString();
        }

        [McpServerTool, Description("Retrieves information on available consultants including their ConsultantId. This can be used to see which consultants are available for consultation with their corresponding ConsultantId. Consultants can provide additional advice, different perspectives on a topic, or advanced/alternative knowledge.")]
        public static IEnumerable<object> GetAvailableConsultants(
            IMcpServer thisServer
            )
        {
            return consultants.Select(c => new { c.ConsultantId, c.ExpertiseLevel, c.Topic });
        }

    }
}
