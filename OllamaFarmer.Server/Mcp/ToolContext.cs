using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;
using OllamaFarmer.Data;
using System;
using IBinaryRepository = OllamaFarmer.Server.Data.IBinaryRepository;
using DataAppDbContext = OllamaFarmer.Data.AppDbContext;

namespace OllamaFarmer.Server.Mcp
{
    public class ToolContext : IDisposable
    {
        public IMcpServer Server { get; }
        public IServiceProvider Services { get; }

        public ILogger Logger { get; }
        private IDisposable? loggerScope;

    public DataAppDbContext AppDbContext => appDbContext.Value ?? throw new InvalidOperationException("AppDbContext is not initialized.");
    Lazy<DataAppDbContext> appDbContext { get; }

        public IBinaryRepository BinaryRepository => binaryRepository.Value ?? throw new InvalidOperationException("BinaryRepository is not available.");
    Lazy<IBinaryRepository> binaryRepository { get; }


        public ToolContext(string context, IMcpServer server, IServiceProvider services)
        {
            if (string.IsNullOrWhiteSpace(context))
            {
                throw new ArgumentException("Context cannot be null or empty.", nameof(context));
            }
            if (server == null)
            {
                throw new ArgumentNullException(nameof(server), "Server cannot be null.");
            }
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services), "Services cannot be null.");
            }
            Server = server ?? throw new ArgumentNullException(nameof(server));
            Services = services ?? throw new ArgumentNullException(nameof(services));
            Logger = services.GetRequiredService<ILogger<ToolContext>>();
            appDbContext = new Lazy<DataAppDbContext>(GetAppDbContext);
            binaryRepository = new Lazy<IBinaryRepository>(() => Services.GetRequiredService<IBinaryRepository>());


            loggerScope = Logger.BeginScope("ToolContext: {Context}", context);
            Logger.LogInformation("ToolContext initialized with context: {Context}", context);
        }



        public T GetService<T>() where T : notnull
        {
            return Services.GetRequiredService<T>();
        }

        public T? GetServiceOrDefault<T>() where T : class
        {
            return Services.GetService<T>();
        }

        private DataAppDbContext GetAppDbContext()
        {
            var dbFactory = Services.GetRequiredService<IDbContextFactory<DataAppDbContext>>();
            var db = dbFactory.CreateDbContext();
            if (db == null)
            {
                Logger.LogError("Database context is null.");
                throw new InvalidOperationException("Database context is null.");
            }
            return db;
        }

        public void Dispose()
        {
            Logger.LogInformation("ToolContext disposed.");
            if (appDbContext.IsValueCreated)
                appDbContext.Value.Dispose();

            loggerScope?.Dispose();
        }

        ~ToolContext()
        {
            // Finalizer to ensure resources are cleaned up if Dispose is not called
            Dispose();
        }

    }
}
