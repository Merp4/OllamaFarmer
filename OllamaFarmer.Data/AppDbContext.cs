using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
//using Microsoft.Extensions.AI;
using OllamaFarmer.Data.Entities;
using System.Text.Json;

namespace OllamaFarmer.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Entities.ChatModel> Models { get; set; } = null!;
    public DbSet<Entities.ChatModelTag> Tags { get; set; } = null!;
    public DbSet<Entities.ChatModelTagLink> ModelTagLinks { get; set; } = null!;
    public DbSet<KvpState> States { get; set; } = null!;

        public DbSet<AppChatEntity> AppChats { get; set; } = null!;
        public DbSet<AppMessageEntity> AppChatMessages { get; set; } = null!;

    public DbSet<Entities.McpServer> McpServers { get; set; } = null!;
    public DbSet<Entities.ChatServer> ChatServers { get; set; } = null!;
    public DbSet<Entities.McpTool> McpTools { get; set; } = null!;

    public DbSet<Entities.ToolBag> ToolBags { get; set; } = null!;
    public DbSet<Entities.ToolBagTool> ToolBagTools { get; set; } = null!;

        public DbSet<Consultant> Consultants { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ChatModel>(e =>
            {
                e.HasKey(c => c.Id);
                e.Property(e => e.Id).ValueGeneratedNever();
                e.HasIndex(c => c.Model).IsUnique();
                e.HasMany(e => e.TagsLinks).WithOne(e => e.Model);
                ConfigureJsonConversion(e.Property(e => e.Capabilities), true);
            });

            modelBuilder.Entity<ChatModelTagLink>(e =>
            {
                e.HasKey(c => c.Id);
                e.Property(c => c.Id).ValueGeneratedNever();
                e.HasOne(e => e.Tag).WithMany(e => e.TagsLinks);
                e.HasOne(e => e.Model).WithMany(e => e.TagsLinks);
            });
            modelBuilder.Entity<ChatModelTag>(e =>
            {
                e.HasKey(c => c.Id);
                e.Property(e => e.Id).ValueGeneratedNever();
                e.HasMany(e => e.TagsLinks).WithOne(e => e.Tag);
            });

            modelBuilder.Entity<Consultant>(e =>
            {
                e.HasKey(c => c.Id);
                e.Property(c => c.Id).ValueGeneratedNever();
                e.Property(c => c.ConsultantId).HasMaxLength(64).IsRequired();
                e.Property(c => c.Name).HasMaxLength(128).IsRequired();
                e.Property(c => c.Topic).HasMaxLength(128).IsRequired();
                e.Property(c => c.SystemMessage);
                e.HasIndex(c => c.ConsultantId).IsUnique();
                e.HasIndex(c => new { c.ChatServerId, c.Name }).IsUnique();
            });

            modelBuilder.Entity<KvpState>(e =>
            {
                e.HasKey(k => k.Key);
                e.Property(k => k.Key).HasMaxLength(256).IsRequired();
                e.Property(k => k.Value).IsRequired();
                e.Property(k => k.UpdatedAt).IsRequired().ValueGeneratedOnAddOrUpdate();
                e.Property(k => k.CreatedAt).IsRequired().ValueGeneratedOnAdd();
                e.Property(k => k.Id).ValueGeneratedNever();
                e.HasIndex(k => k.Key).IsUnique();
            });

            modelBuilder.Entity<McpServer>(e =>
            {
                e.HasKey(m => m.Id);
                e.Property(m => m.Id).ValueGeneratedNever();
                e.Property(m => m.Name).IsRequired().HasMaxLength(256);
                e.Property(m => m.Uri).IsRequired();
                e.HasMany(m => m.McpTools).WithOne().HasForeignKey(t => t.McpServerId);
            });
            modelBuilder.Entity<ChatServer>(e =>
            {
                e.HasKey(m => m.Id);
                e.Property(m => m.Id).ValueGeneratedNever();
                e.Property(m => m.Name).IsRequired().HasMaxLength(256);
                e.Property(m => m.Uri).IsRequired();
                e.HasMany(m => m.ChatModels).WithOne().HasForeignKey(m => m.ChatServerId);
            });

            modelBuilder.Entity<ToolBag>(e =>
            {
                e.HasKey(m => m.Id);
                e.Property(m => m.Id).ValueGeneratedNever();
                e.Property(m => m.Name).IsRequired().HasMaxLength(256);
                e.HasMany(m => m.Tools).WithOne().HasForeignKey(t => t.ToolBagId);
            });

            modelBuilder.Entity<ToolBagTool>(e =>
            {
                e.HasKey(m => m.Id);
                e.Property(m => m.Id).ValueGeneratedNever();
                e.Property(m => m.ToolBagId).IsRequired();
                e.Property(m => m.McpToolId).IsRequired();
                e.HasIndex(m => new { m.ToolBagId, m.McpToolId }).IsUnique();
            });

            modelBuilder.Entity<AppChatEntity>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedNever();
                entity.HasMany(c => c.Messages).WithOne().OnDelete(DeleteBehavior.Cascade);
                ConfigureJsonConversion<AppChatOptionsEntity>(entity.Property(e => e.Options));
                ConfigureJsonConversion<Entities.ModelCapabilities>(entity.Property(e => e.ModelCapabilities));
            });

            modelBuilder.Entity<AppMessageEntity>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedNever();
                entity.Property(e => e.Index);
                entity.Property(e => e.CreatedAt);
                // Store role as string
                entity.Property(e => e.Role);
                entity.Property(e => e.Content);
                entity.HasOne(m => m.AppChat).WithMany(c => c.Messages).HasForeignKey(m => m.AppChatEntityId).OnDelete(DeleteBehavior.Cascade);
                ConfigureJsonConversion<List<AppImageEntity>>(entity.Property(e => e.Images));
                ConfigureJsonConversion<List<string>>(entity.Property(e => e.Tools));
                ConfigureJsonConversion<List<string>>(entity.Property(e => e.ImageUrls));
                entity.Property(e => e.ToolCallArgs).HasConversion(
                    v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
                    v => JsonSerializer.Deserialize<Dictionary<string, object?>>(v, JsonSerializerOptions.Default));
            });
        }

        public static PropertyBuilder<T> ConfigureJsonConversion<T>(PropertyBuilder<T> propertyBuilder, bool supressErrors = false)
            where T : class, new()
        {
            if (propertyBuilder == null) throw new ArgumentNullException(nameof(propertyBuilder));
            return propertyBuilder.HasConversion(
                        v => JsonSerializer.Serialize(v, new JsonSerializerOptions()),
                        v => string.IsNullOrEmpty(v) ? new T() : (Deserailise<T>(v, supressErrors)) ?? new T());
        }

        private static T? Deserailise<T>(string v, bool supressErrors) where T : class, new()
        {
            try
            {
                return JsonSerializer.Deserialize<T>(v, new JsonSerializerOptions() { });
            }
            catch (Exception ex)
            {
                if (!supressErrors) throw new InvalidOperationException($"Failed to deserialize JSON to {typeof(T).Name}", ex);
                return null;
            }
        }
    }
}
