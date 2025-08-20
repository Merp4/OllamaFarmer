using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.AI;
using OllamaFarmer.Server.Data.Entities;
using OllamaFarmer.Server.Models;
using OllamaFarmer.Server.Services.Ollama;
using OllamaSharp;
//using OllamaSharp.Models.Chat;
using System.Text.Json;

namespace OllamaFarmer.Server.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }
        public DbSet<ChatModel> Models { get; set; } = null!;
        public DbSet<ChatModelTag> Tags { get; set; } = null!;
        public DbSet<ChatModelTagLink> ModelTagLinks { get; set; } = null!;
        public DbSet<KvpState> States { get; set; } = null!;

        public DbSet<AppChatEntity> AppChats { get; set; } = null!;
        public DbSet<AppMessageEntity> AppChatMessages { get; set; } = null!;

        public DbSet<McpServer> McpServers { get; set; } = null!;
        public DbSet<ChatServer> ChatServers { get; set; } = null!;
        public DbSet<McpTool> McpTools { get; set; } = null!;

        public DbSet<ToolBag> ToolBags { get; set; } = null!;
        public DbSet<ToolBagTool> ToolBagTools { get; set; } = null!;

        public DbSet<Consultant> Consultants { get; set; } = null!;


        // MariaDB issue - handled in this library but not EF Core/main provider
        // https://github.com/PomeloFoundation/Pomelo.EntityFrameworkCore.MySql/blob/24a3b5bb2c0ac6839e5035eba1925dff4ac6140a/src/EFCore.MySql/Migrations/Internal/MySqlHistoryRepository.cs#L68

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);


            modelBuilder.Entity<ChatModel>(e =>
            {
                e.HasKey(c => c.Id);
                e.Property(e => e.Id).ValueGeneratedNever();

                e.HasIndex(c => c.Model).IsUnique();
                e.HasMany(e => e.TagsLinks)
                    .WithOne(e => e.Model);
                // long isn't mapping when migrating... - driver issue, new pomelo works - todo
                //e.Property(ep => ep.Size).HasColumnType("bigint");

                ConfigureJsonConversion(e.Property(e => e.Capabilities), true);



            });
            modelBuilder.Entity<ChatModelTagLink>(e =>
            {
                e.HasKey(c => c.Id);
                e.Property(c => c.Id).ValueGeneratedNever();

                e.HasOne(e => e.Tag)
                    .WithMany(e => e.TagsLinks);
                e.HasOne(e => e.Model)
                    .WithMany(e => e.TagsLinks);
            });
            modelBuilder.Entity<ChatModelTag>(e =>
            {
                e.HasKey(c => c.Id);
                e.Property(e => e.Id).ValueGeneratedNever();

                e.HasMany(e => e.TagsLinks)
                    .WithOne(e => e.Tag);
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
                // We need to add a unique index on Key to prevent duplicates
                e.HasIndex(k => k.Key).IsUnique();
            });


            // McpServer and ChatServer etc.

            modelBuilder.Entity<McpServer>(e =>
            {
                e.HasKey(m => m.Id);
                e.Property(m => m.Id).ValueGeneratedNever();
                e.Property(m => m.Name).IsRequired().HasMaxLength(256);
                e.Property(m => m.Uri).IsRequired();
                e.HasMany(m => m.McpTools)
                    .WithOne()
                    .HasForeignKey(t => t.McpServerId);

            });
            modelBuilder.Entity<ChatServer>(e =>
            {
                e.HasKey(m => m.Id);
                e.Property(m => m.Id).ValueGeneratedNever();
                e.Property(m => m.Name).IsRequired().HasMaxLength(256);
                e.Property(m => m.Uri).IsRequired();
                e.HasMany(m => m.ChatModels)
                    .WithOne()
                    .HasForeignKey(m => m.ChatServerId);
            });

            modelBuilder.Entity<ToolBag>(e =>
            {
                e.HasKey(m => m.Id);
                e.Property(m => m.Id).ValueGeneratedNever();
                e.Property(m => m.Name).IsRequired().HasMaxLength(256);
                e.HasMany(m => m.Tools)
                    .WithOne()
                    .HasForeignKey(t => t.ToolBagId);
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
                entity.HasMany(c => c.Messages)
                    .WithOne()
                    .OnDelete(DeleteBehavior.Cascade);

                // Serialize Options as JSON
                ConfigureJsonConversion<AppChatOptionsEntity>(entity.Property(e => e.Options));
                ConfigureJsonConversion<ModelCapabilities>(entity.Property(e => e.ModelCapabilities));
            });

            modelBuilder.Entity<AppMessageEntity>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.Index);
                entity.Property(e => e.CreatedAt);
                
                // Configure ChatRole struct to JSON string conversion
                entity.Property(e => e.Role)
                    .HasConversion(
                        v => v.HasValue ? JsonSerializer.Serialize(v.Value, new JsonSerializerOptions()) : null,
                        v => string.IsNullOrEmpty(v) ? null : JsonSerializer.Deserialize<ChatRole>(v, new JsonSerializerOptions()));
                    
                entity.Property(e => e.Content);
                
                // Configure relationship with AppChatEntity
                entity.HasOne(m => m.AppChat)
                    .WithMany(c => c.Messages)
                    .HasForeignKey(m => m.AppChatEntityId)
                    .OnDelete(DeleteBehavior.Cascade);
                
                // Serialize complex objects as JSON
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
            // Todo: supressErrors
            if (propertyBuilder == null)
                throw new ArgumentNullException(nameof(propertyBuilder));

            return propertyBuilder.HasConversion(
                        v => JsonSerializer.Serialize(v, new JsonSerializerOptions()),
                        v => string.IsNullOrEmpty(v) ? new T() :
                                (Deserailise<T>(v, supressErrors)) ?? new T());
        }

        private static T? Deserailise<T>(string v, bool supressErrors) where T : class, new()
        {
            try
            {
                return JsonSerializer.Deserialize<T>(v, new JsonSerializerOptions() { });
            }
            catch (Exception ex)
            {
                if (!supressErrors)
                    throw new InvalidOperationException($"Failed to deserialize JSON to {typeof(T).Name}", ex);
                // Log error or handle it as needed
                return null;
            }
        }
    }
}
