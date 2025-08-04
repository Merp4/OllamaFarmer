namespace OllamaFarmer.Server.Data.Entities
{
    public class AppImageEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string? Url { get; set; }
        public string? Path { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? MediaType { get; internal set; }
    }
}
