using Microsoft.EntityFrameworkCore;

namespace OllamaFarmer.Server.Data
{
    // Forwarder class so existing migrations referencing Server.Data.AppDbContext continue to compile.
    public class AppDbContext : OllamaFarmer.Data.AppDbContext
    {
        public AppDbContext(DbContextOptions<OllamaFarmer.Data.AppDbContext> options) : base(options) { }
    }
}
