namespace OllamaFarmer.Server.Dto
{
    public class CreateChatDto
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public string? Model { get; set; }
        public string? SystemMessage { get; set; }
        public float Temperature { get; set; } = 0.7f;
        public float TopP { get; set; } = 1.0f;
        public float FrequencyPenalty { get; set; } = 0.0f;
        public float PresencePenalty { get; set; } = 0.0f;
        public Guid ServerId { get; set; }
        public List<Guid> EnabledToolIds { get; set; } = new(); // MCP tool IDs to enable for this chat
    }
}
