using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OllamaFarmer.Server.Data;
using OllamaFarmer.Server.Data.Entities;

namespace OllamaFarmer.Server.Services.Hosted
{
    public class DbDefaults
    {
        public string DefaultChatServerName { get; set; } = "Local Ollama Server";
        public string DefaultChatServerDescription { get; set; } = "Default local Ollama server instance";
        public Uri DefaultChatServerUri { get; set; } = new Uri("http://localhost:11434");
        public string DefaultMcpServerName { get; set; } = "Local MCP Server";
        public string DefaultMcpServerDescription { get; set; } = "Default local MCP server instance";
        public Uri DefaultMcpServerUri { get; set; } = new Uri("http://localhost:5280/api/mcp/");

        public DbDefaults()
        {
            // Default values are already set in properties
        }
        public DbDefaults(IConfiguration configuration) {
            // Load defaults from configuration if available
            DefaultChatServerName = configuration["DefaultChatServer:Name"] ?? DefaultChatServerName;
            DefaultChatServerDescription = configuration["DefaultChatServer:Description"] ?? DefaultChatServerDescription;
            var chatServerUri = configuration["DefaultChatServer:Uri"];
            if (chatServerUri != null)
            {
                DefaultChatServerUri = new Uri(chatServerUri);
            }
            DefaultMcpServerName = configuration["DefaultMcpServer:Name"] ?? DefaultMcpServerName;
            DefaultMcpServerDescription = configuration["DefaultMcpServer:Description"] ?? DefaultMcpServerDescription;
            var mcpServerUri = configuration["DefaultMcpServer:Uri"];
            if (mcpServerUri != null)
            {
                DefaultMcpServerUri = new Uri(mcpServerUri);
            }
        }
    }

    public class DbSeedingService : IHostedService
    {
        private readonly IDbContextFactory<AppDbContext> _dbFactory;
        private readonly ILogger<DbSeedingService> _logger;
        private readonly DbDefaults _dbDefaults;

        public DbSeedingService(IDbContextFactory<AppDbContext> dbFactory, ILogger<DbSeedingService> logger, DbDefaults? dbDefaults = null)
        {
            _dbFactory = dbFactory;
            _logger = logger;
            _dbDefaults = dbDefaults ?? new();
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("DbSeedingService starting");

            try
            {
                using var dbContext = _dbFactory.CreateDbContext();

                await SeedDefaultChatServerAsync(dbContext);

                _logger.LogInformation("DbSeedingService completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during database seeding");
            }
        }

        private async Task SeedDefaultChatServerAsync(AppDbContext dbContext)
        {
            // Check if any chat servers exist
            var existingServers = await dbContext.ChatServers.AnyAsync();

            if (!existingServers)
            {
                _logger.LogInformation("No chat servers found, creating default localhost server");

                var defaultServer = new ChatServer
                {
                    Id = Guid.NewGuid(),
                    Name = _dbDefaults.DefaultChatServerName,
                    Description = _dbDefaults.DefaultChatServerDescription,
                    Uri = _dbDefaults.DefaultChatServerUri,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                };

                dbContext.ChatServers.Add(defaultServer);
                await dbContext.SaveChangesAsync();

                _logger.LogInformation("Created default chat server: {ServerName} at {ServerUri}",
                    defaultServer.Name, defaultServer.Uri);
            }
            else
            {
                _logger.LogInformation("Chat servers already exist, skipping default server creation");
            }

            var existingMcpServers = await dbContext.McpServers.AnyAsync();

            if (!existingMcpServers)
            {
                _logger.LogInformation("No MCP servers found, creating default localhost server");
                var defaultMcpServer = new McpServer
                {
                    Id = Guid.NewGuid(),
                    Name = _dbDefaults.DefaultMcpServerName,
                    Description = _dbDefaults.DefaultMcpServerDescription,
                    Uri = _dbDefaults.DefaultMcpServerUri,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                };
                dbContext.McpServers.Add(defaultMcpServer);
                await dbContext.SaveChangesAsync();
                _logger.LogInformation("Created default MCP server: {ServerName} at {ServerUri}",
                    defaultMcpServer.Name, defaultMcpServer.Uri);
            }
            else
            {
                _logger.LogInformation("MCP servers already exist, skipping default server creation");
            }

        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("DbSeedingService stopping");
            return Task.CompletedTask;
        }
    }
}
