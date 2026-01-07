using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using MyVocaList.Infra.Data.Interceptors;

namespace MyVocaList.Infra.Data
{
    /// <summary>
    /// Factory for creating AppDbContext at design-time (for migrations)
    /// </summary>
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();

            // For design-time, use absolute path in temporary directory
            string dbPath = Path.Combine(Path.GetTempPath(), "myvocalist_design.db");

            // Add CollationInterceptor to ensure NOCASE_NOACCENT collation is registered for migrations
            optionsBuilder.UseSqlite($"Data Source={dbPath}")
                          .AddInterceptors(new CollationInterceptor());

            return new AppDbContext(optionsBuilder.Options);
        }
    }
}
