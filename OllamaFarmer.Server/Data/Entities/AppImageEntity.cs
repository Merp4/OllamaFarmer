//using OllamaSharp.Models.Chat;

namespace OllamaFarmer.Server.Data.Entities
{
    //public class AppImageLink
    //{
    //    public AppMessage Message { get; set; }
    //    public AppImage Image { get; set; }
    //}

    public class AppImageEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string? Url { get; set; }
        public string? Path { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? MediaType { get; internal set; }
        //public List<AppImageLink> Messages { get; set; } = new();
    }
}
