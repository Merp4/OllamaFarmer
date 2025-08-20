using System;
using System.ComponentModel.DataAnnotations;

namespace OllamaFarmer.Server.Data.Entities
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
        public Guid Id { get; set; } = Guid.NewGuid();

        // System-generated identifier used by tools (e.g. "CONS-ABCD1234")
        [MaxLength(64)]
        public string ConsultantId { get; set; } = string.Empty;

        // User-configurable display name for UI
        [MaxLength(128)]
        public string Name { get; set; } = string.Empty;

        // Logical grouping/topic
        [MaxLength(128)]
        public string Topic { get; set; } = string.Empty;

        // FK -> ChatModel (maps consultant to a model)
        public Guid ChatModelId { get; set; }

        // Cached server for fast filtering; should match ChatModel.ChatServerId
        public Guid ChatServerId { get; set; }

        public string? SystemMessage { get; set; }
        public ConsultantExpertiseLevel ExpertiseLevel { get; set; }

        public float Temperature { get; set; } = 0.7f;
        public float TopP { get; set; } = 1.0f;
        public float FrequencyPenalty { get; set; } = 0.0f;
        public float PresencePenalty { get; set; } = 0.0f;

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}
