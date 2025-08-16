namespace OllamaFarmer.Server.Data.Entities
{
    public class ToolBag
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

        public List<ToolBagTool> Tools { get; set; } = new();
    }

    public class ToolBagTool
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ToolBagId { get; set; }
        public Guid McpToolId { get; set; }

        // Optional metadata
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}
