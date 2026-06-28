using Microsoft.EntityFrameworkCore;
using MyVocaList.Domain.Entity;
using MyVocaList.Domain.Interfaces;
using MyVocaList.Infra.Collation;
using MyVocaList.Infra.EntityEFConfig;
using QueueManagementEvent = MyVocaList.Domain.Entities.Event;
using QueueManagementQueueEntry = MyVocaList.Domain.Entities.QueueEntry;

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
    public DbSet<Artist> Artists { get; set; }
    public DbSet<Song> Songs { get; set; }
    public DbSet<Catalog> Catalog { get; set; }
    public DbSet<SongKaraokeUrl> SongKaraokeUrls { get; set; }
    public DbSet<BackupHistory> BackupHistories { get; set; }
    public DbSet<QueueManagementEvent> QueueManagementEvents { get; set; }
    public DbSet<QueueManagementQueueEntry> QueueEntries { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
        // Collation registration is handled automatically by CollationInterceptor
        // The interceptor registers NOCASE_NOACCENT on every connection (including migrations)
        // No need for manual registration here anymore

        // Global NoTracking default (BUG-018) — prevents ChangeTracker pollution in concurrent queries
        // Explicit .AsNoTracking() on list methods provides defence-in-depth
        // Edit queries use explicit .AsTracking() to enable change detection
        ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
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

            optionsBuilder
                .UseSqlite($"Data Source={tempPath}")
                .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
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
        modelBuilder.ApplyConfiguration(new ArtistConfiguration());
        modelBuilder.ApplyConfiguration(new SongConfiguration());
        modelBuilder.ApplyConfiguration(new CatalogConfiguration());
        modelBuilder.ApplyConfiguration(new SongKaraokeUrlConfiguration());
        modelBuilder.ApplyConfiguration(new BackupHistoryConfiguration());
        modelBuilder.ApplyConfiguration(new QueueManagementEventConfiguration());
        modelBuilder.ApplyConfiguration(new QueueEntryConfiguration());
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
    private static void SetDatabaseCollation(ModelBuilder modelBuilder)
    {
        // For SQLite: Apply custom NOCASE_NOACCENT collation to all string properties
        // The collation is registered automatically by CollationInterceptor on every connection
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(string))
                {
                    property.SetCollation(CollationConstants.Default);
                }
            }
        }
    }
}
