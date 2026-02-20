using Microsoft.EntityFrameworkCore;
using MyVocaList.Domain.Entity;
using MyVocaList.Infra.EntityEFConfig;

namespace MyVocaList.Infra;

/// <summary>
/// Application database context for MyVocaList
/// </summary>
public class AppDbContext : DbContext
{
    public DbSet<Person> People { get; set; }
    public DbSet<Venue> Venues { get; set; }
    public DbSet<Event> Events { get; set; }
    public DbSet<EventParticipation> EventParticipations { get; set; }
    public DbSet<SystemConfiguration> SystemConfigurations { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
        // Collation registration is handled automatically by CollationInterceptor
        // The interceptor registers NOCASE_NOACCENT on every connection (including migrations)
        // No need for manual registration here anymore
    }

    /// <summary>
    /// Empty constructor for migrations only
    /// </summary>
    public AppDbContext() { }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            // Design-time/migrations only - use temporary path
            var tempPath = Path.Combine(Path.GetTempPath(), "myvocalist_design.db");

            optionsBuilder.UseSqlite($"Data Source={tempPath}");
        }
    }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Set database-level default collation
        // This applies case and accent insensitive collation to ALL string columns automatically
        // Supports: João = joao = JOAO = jOãO (any case/accent combination)
        SetDatabaseCollation(modelBuilder);

        // Apply entity configurations
        modelBuilder.ApplyConfiguration(new PersonConfiguration());
        modelBuilder.ApplyConfiguration(new VenueConfiguration());
        modelBuilder.ApplyConfiguration(new EventConfiguration());
        modelBuilder.ApplyConfiguration(new EventParticipationConfiguration());
        modelBuilder.ApplyConfiguration(new SystemConfigurationConfiguration());
    }

    /// <summary>
    /// Sets database-level default collation for all string columns.
    /// SQLite: Custom NOCASE_NOACCENT collation (case + accent insensitive)
    ///
    /// REGISTRATION: The collation itself is registered by CollationInterceptor on every connection.
    /// This method only tells EF Core to USE that collation for string comparisons.
    ///
    /// FUTURE: When migrating to SQL Server, use:
    /// - SQL Server: Latin1_General_CI_AI (CI = Case Insensitive, AI = Accent Insensitive)
    /// - PostgreSQL: und-u-ks-level1 (ICU collation, ignores case and accents)
    /// </summary>
    private void SetDatabaseCollation(ModelBuilder modelBuilder)
    {
        // For SQLite: Apply custom NOCASE_NOACCENT collation to all string properties
        // The collation is registered automatically by CollationInterceptor on every connection

        // Apply to all string properties in all entities
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(string))
                {
                    property.SetCollation("NOCASE_NOACCENT");
                }
            }
        }

        Console.WriteLine("✅ Database collation configured: NOCASE_NOACCENT (registered by CollationInterceptor)");
    }

    /// <summary>
    /// Debug method to get the actual database file path
    /// </summary>
    public string GetDatabasePath()
    {
        var connection = Database.GetDbConnection();
        return connection.DataSource;
    }

    /// <summary>
    /// Debug method to log database information
    /// </summary>
    public void LogDatabaseInfo()
    {
        var connection = Database.GetDbConnection();
        Console.WriteLine($"🗃️ Database Path: {connection.DataSource}");
        Console.WriteLine($"🗃️ Connection State: {connection.State}");
        Console.WriteLine($"🗃️ Database Type: {Database.ProviderName}");
    }
}
