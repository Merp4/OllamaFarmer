using Microsoft.AspNetCore.Connections;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OllamaFarmer.Server.Data;
using OllamaFarmer.Server.Services.Hosted;
using System.Text.RegularExpressions;

namespace OllamaFarmer.Server.Services.Hosted
{
    public class DbMigrationService<TDbContext> : IHostedService
        where TDbContext : DbContext
    {
        private IDbContextFactory<TDbContext> dbFactory;
        private readonly ILogger<DbMigrationService<TDbContext>> _logger;

        public DbMigrationService(IDbContextFactory<TDbContext> dbFactory, ILogger<DbMigrationService<TDbContext>> logger)
        {
            this.dbFactory = dbFactory;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"DbContext {typeof(TDbContext).Name} migration starting");

            var dbContext = dbFactory.CreateDbContext();

            //#if DEBUG
            //            _logger.LogWarning($"DbContext {typeof(TDbContext).Name} Ensure deleted");
            //            await dbContext.Database.EnsureDeletedAsync();
            //#endif

            //_logger.LogInformation($"DbContext {typeof(TDbContext).Name} Ensure created");
            //await dbContext.Database.EnsureCreatedAsync(); // migrate seems to create DB also
            // this possibly causes another issue with longs/bigints


            if (!dbContext.Database.GetMigrations().Any())
            {
                _logger.LogWarning($"DbContext {typeof(TDbContext).Name} has no migrations present");
                return;
            }

            var x = (await dbContext.Database.GetPendingMigrationsAsync()).ToList();

            if (x.Any())
            {
                _logger.LogInformation($"DbContext {typeof(TDbContext).Name} applying {x.Count} migrations");
                await dbContext.Database.MigrateAsync();
            }
            else
            {
                _logger.LogInformation($"DbContext {typeof(TDbContext).Name} up to date");
            }

            _logger.LogInformation($"DbContext {typeof(TDbContext).Name} migration complete");
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"DbContext {typeof(TDbContext).Name} migrator shutting down");
        }
    }


    public static class EfCoreExtensions
    {
        public static void AddMySQLServerAndMigrationService<TContext>(this IServiceCollection services, string connectionString)
            where TContext : DbContext
        {
            //services.AddMySQLServer<TContext>(connectionString);
            services.AddDbContextFactory<TContext>(options => options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));
            //services.AddDbContext<AppDbContext>(options => options.UseMySQL(connectionString));
            //services.AddDbContext<AppDbContext>(options => options.UseMySQL(connectionString));
            services.AddHostedService<DbMigrationService<TContext>>();
        }

        public static void AddDbSeedingService(this IServiceCollection services)
        {
            services.AddHostedService<DbSeedingService>();
        }

        private static string GetDbName<T>()
        {
            var name = typeof(T).Name;
            Regex.Replace(name, "[^0-9.]", string.Empty);
            Regex.Replace(name, "(DbContext|Context)$", string.Empty);
            return name;
        }
    }
}
