using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace OllamaFarmer.Data;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        var serverVersion = new MySqlServerVersion(new Version(9, 3, 0));
        optionsBuilder.UseMySql(serverVersion, options =>
        {
            options.MigrationsAssembly("OllamaFarmer.Server");
            options.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorNumbersToAdd: null);
        });
        optionsBuilder.EnableSensitiveDataLogging();
        optionsBuilder.EnableDetailedErrors();
        return new AppDbContext(optionsBuilder.Options);
    }
}
