using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json.Converters;
using OllamaFarmer.Server.Data;
using OllamaFarmer.Server.Services;
using OllamaFarmer.Server.Services.Background;
using OllamaFarmer.Server.Services.Interfaces;
using OllamaFarmer.Server.Data.Interfaces;
using OllamaFarmer.Server.Services.Background.Interfaces;
using OllamaFarmer.Server.Services.Hosted;
using OllamaFarmer.Server.Services.Mcp;
using OllamaFarmer.Server.Services.Mcp.Interfaces;
using OllamaFarmer.Server.Services.Ollama;
using OllamaFarmer.Server.Services.Ollama.Interfaces;
using OllamaFarmer.Server.Services.SignalR;
using OllamaFarmer.Server.Utils;
using OllamaSharp;
using OllamaSharp.ModelContextProtocol.Server;
using Serilog;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;

namespace OllamaFarmer.Server
{

    public class Program
    {
        private const string ConnectionStringName = "MySql";

        public static async Task Main(string[] args)
        {
            var builder = CreateHostBuilder(args);

            // Configure services
            // Add services to the container.
            ConfigureServices(builder.Services, builder.Configuration, builder.Environment.IsDevelopment());

            // Signal R
            builder.Services.AddSignalR(o =>
            {
                if (builder.Environment.IsDevelopment())
                    o.EnableDetailedErrors = true;
                else
                    o.EnableDetailedErrors = false;

            }).AddJsonProtocol(o => o.PayloadSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter(JsonNamingPolicy.CamelCase)));

            // Endpoint rate limiting
            ConfigureRateLimiting(builder);


            // App configuration
            var app = builder.Build();

            app.UseDefaultFiles();
            app.MapStaticAssets(); // https://learn.microsoft.com/en-us/aspnet/core/fundamentals/map-static-files?view=aspnetcore-9.0#mapstaticassets-versus-usestaticfiles
            app.UseStaticFiles(); // SPA

            if (app.Environment.IsDevelopment())
            {
                Console.WriteLine("Running in Development mode");

                // For local dev, allow CORS via SPA proxy
                app.UseCors(opts => opts
                    .WithOrigins("https://localhost:44004")
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials()
                );
                app.MapOpenApi();

                // documention metadata:
                // https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/include-metadata?view=aspnetcore-9.0&tabs=minimal-apis#include-openapi-metadata-for-endpoints
            }
            else
            {
                app.UseExceptionHandler("/error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            // Endpoints
            app.MapControllers();
            app.MapHub<NotificationHub>("/api/hubs/notification", opts => { });
            app.MapMcp("/api/mcp");

            // SPA fallback - exclude API routes
            app.MapFallbackToFile("/{*path:nonfile}", "index.html");

            await app.RunAsync();
        }


        // EF Core design-time tooling (~required) & OpenAPI design-time tooling (optional) need this method signature
        private static WebApplicationBuilder CreateHostBuilder(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Serilog configuration
            builder.Host.UseSerilog((context, services, config) =>
            {
                if (builder.Environment.IsDevelopment())
                    config.ReadFrom.Configuration(context.Configuration)
                          .Enrich.FromLogContext()
                          .WriteTo.Console();
                else
                    config.ReadFrom.Configuration(context.Configuration)
                          .Enrich.FromLogContext()
                          .WriteTo.Console();
            });


            // Endpoints
            builder.Services.AddControllers()
                .AddJsonOptions(opts =>
                {
                    // Use System.Text.Json for serialization
                    opts.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
                    opts.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                    opts.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
                });
            builder.Services.AddOpenApi(opts =>
            {
                opts.AddSchemaTransformer<StringEnumSchemaTransformer>();
            }); // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi


            // EF Core - Design-time tooling requires this here
            builder.Services.AddMySQLServerAndMigrationService<AppDbContext>(
                builder.Configuration.GetConnectionString(ConnectionStringName) ?? throw new InvalidOperationException($"Connection string '{ConnectionStringName}' not found."));



            return builder;
        }


        private static void ConfigureServices(IServiceCollection services, IConfigurationRoot config, bool v)
        {
            // Background processing
            int bgTaskLimit = 50;
            int bgThreadCount = 2;// Environment.ProcessorCount;
            services.AddHostedService(sp =>
                new BackgroundTaskService(
                    sp.GetRequiredService<IBackgroundTaskQueue>(),
                    sp.GetRequiredService<ILogger<BackgroundTaskService>>(),
                    bgThreadCount));
            services.AddSingleton<IBackgroundTaskQueue>(sp => new BackgroundTaskQueue(bgTaskLimit));


            // Ollama API
            services.AddSingleton<IOllamaApiClientFactory, OllamaApiClientFactory>(sp => new OllamaApiClientFactory());

            // MCP
            services.AddMcpServer().WithHttpTransport().WithToolsFromAssembly();

            // Caching
            services.AddSingleton<IMemoryCache, MemoryCache>(s =>
                new MemoryCache(Options.Create(new MemoryCacheOptions { }), s.GetRequiredService<ILoggerFactory>()));

            // Database seeding
            services.AddDbSeedingService();

            // App services
            services.AddTransient<IOllamaModelService, OllamaModelService>();
            services.AddTransient<IChatServerRepository, ChatServerRepository>();
            services.AddTransient<IMcpServerRepository, McpServerRepository>();
            services.AddTransient<IMcpToolService, McpToolService>();
            services.AddTransient<IMcpClientProvider, McpClientProvider>();


            // With the following code to fix CS1501:
            services.AddTransient<IBinaryRepository, BinaryRepository>(sp =>
            {
                var configSection = config.GetSection("FileRepository");
                var repoConfig = new BinaryRepositoryConfiguration();
                configSection.Bind(repoConfig);
                return new BinaryRepository(sp.GetRequiredService<ILogger<BinaryRepository>>(), repoConfig);
            });


            services.AddSingleton<IAppChatService, AppChatService>();
            services.AddTransient<AppChatServiceOptions>(_ => new());
        }


        private static void ConfigureRateLimiting(WebApplicationBuilder builder)
        {
            builder.Services.AddRateLimiter(options =>
            {
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
                    http => RateLimitPartition.GetFixedWindowLimiter(
                        http.Connection.RemoteIpAddress?.ToString() ?? "none", ip =>
                        {
                            // Configure rate limiting per IP address
                            return new FixedWindowRateLimiterOptions
                            {                                
                                PermitLimit = 100, // Allow 100 requests per window
                                Window = TimeSpan.FromMinutes(1) // 1 minute window
                            };
                        }
                    )
                );
            });
        }
    }
}


