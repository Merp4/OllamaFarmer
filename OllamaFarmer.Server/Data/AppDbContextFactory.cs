using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace OllamaFarmer.Server.Data;

/// <summary>
/// Design-time factory for creating AppDbContext instances during EF migrations.
/// This prevents the EF tooling from attempting to connect to the database during design-time operations.
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        
        // Use a specific MySQL version instead of AutoDetect to avoid connection attempts
        var serverVersion = new MySqlServerVersion(new Version(9, 3, 0));
        optionsBuilder.UseMySql(serverVersion, options =>
        {
            options.MigrationsAssembly("OllamaFarmer.Server");
            options.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorNumbersToAdd: null);
        });

        // Enable sensitive data logging in development
        optionsBuilder.EnableSensitiveDataLogging();
        optionsBuilder.EnableDetailedErrors();

        return new AppDbContext(optionsBuilder.Options);
    }
}
