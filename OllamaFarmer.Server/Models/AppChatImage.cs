using OllamaFarmer.Server.Data.Entities;

namespace OllamaFarmer.Server.Models
{
    /// <summary>
    /// Domain model for chat images - used by business logic layer
    /// </summary>
    public class AppChatImage
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string? Url { get; set; }
        public string? Path { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? MediaType { get; set; }

        /// <summary>
        /// Convert from database entity to domain model
        /// </summary>
        public static AppChatImage FromEntity(AppImageEntity entity)
        {
            return new AppChatImage
            {
                Id = entity.Id,
                Url = entity.Url,
                Path = entity.Path,
                Description = entity.Description,
                CreatedAt = entity.CreatedAt,
                MediaType = entity.MediaType
            };
        }

        /// <summary>
        /// Convert to database entity
        /// </summary>
        public AppImageEntity ToEntity()
        {
            return new AppImageEntity
            {
                Id = Id,
                Url = Url,
                Path = Path,
                Description = Description,
                CreatedAt = CreatedAt,
                MediaType = MediaType
            };
        }
    }
}
