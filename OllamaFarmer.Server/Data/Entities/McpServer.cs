namespace OllamaFarmer.Server.Data.Entities
{

    public class ChatServer
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Version { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

        public required Uri Uri { get; set; }

        public List<ChatModel>? ChatModels { get; set; } = new List<ChatModel>();
    }



    public class McpServer
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Version { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

        public required Uri Uri { get; set; }

        public List<McpTool> McpTools { get; set; } = new List<McpTool>();
    }

    public class McpTool
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid McpServerId { get; set; }

        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Version { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

        //public required Uri Uri { get; set; }
        //public McpServer McpServer { get; set; } = null!;
    }
}
