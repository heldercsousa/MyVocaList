using MyVocaList.Infra;
using MyVocaList.Infra.Repositories;
using MyVocaList.Tests.Infrastructure;
using Event = MyVocaList.Domain.Entities.Event;

namespace MyVocaList.Tests.Integration.Repositories;

/// <summary>
/// Integration tests for EventRepository using real SQLite.
/// Tests CRUD operations, search/filter queries, and EF Core configuration.
/// </summary>
public class EventRepositoryTests : IAsyncLifetime
{
    private AppDbContext _db = null!;
    private EventRepository _repo = null!;

    public async Task InitializeAsync()
    {
        _db = TestDbContextFactory.Create();
        await _db.Database.EnsureCreatedAsync();
        _repo = new EventRepository(_db);
    }

    public async Task DisposeAsync()
    {
        await _db.Database.EnsureDeletedAsync();
        await _db.DisposeAsync();
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    private async Task<Venue> CreateVenueAsync(string name)
    {
        var venue = new Venue { Name = name };
        _db.Set<Venue>().Add(venue);
        await _db.SaveChangesAsync();
        return venue;
    }

    private async Task<Event> CreateEventInDbAsync(string name, Venue venue)
    {
        var @event = new Event
        {
            Name = name,
            VenueId = venue.Id,
            Venue = venue,
            ScheduledStartTime = new DateTime(2026, 6, 15, 20, 0, 0)
        };
        _db.QueueManagementEvents.Add(@event);
        await _db.SaveChangesAsync();
        return @event;
    }

    // ── AddAsync ──────────────────────────────────────────────────────────────

    /// <summary>
    /// [AC] REQ-EVENT-01: Verify AddAsync persists event with auto-timestamps.
    /// </summary>
    [Fact]
    public async Task AddAsync_ValidEvent_PersistedAndReturnedById()
    {
        var venue = await CreateVenueAsync("Jazz Club");

        var @event = new Event
        {
            Name = "Karaoke Night",
            VenueId = venue.Id,
            Venue = venue,
            ScheduledStartTime = new DateTime(2026, 6, 15, 20, 0, 0)
        };

        await _repo.AddAsync(@event, CancellationToken.None);
        var found = await _repo.GetByIdAsync(@event.Id, CancellationToken.None);

        Assert.NotNull(found);
        Assert.Equal("Karaoke Night", found.Name);
        Assert.NotEqual(DateTime.MinValue, found.CreatedAt);
        Assert.NotEqual(DateTime.MinValue, found.ModifiedAt);
    }

    /// <summary>
    /// Verify AddAsync sets CreatedAt and ModifiedAt timestamps.
    /// </summary>
    [Fact]
    public async Task AddAsync_SetsTimestamps()
    {
        var venue = await CreateVenueAsync("Test Venue");
        var now = DateTime.UtcNow;
        var @event = new Event
        {
            Name = "Test Event",
            VenueId = venue.Id,
            Venue = venue,
            ScheduledStartTime = new DateTime(2026, 6, 15, 20, 0, 0)
        };

        await _repo.AddAsync(@event, CancellationToken.None);

        Assert.True(@event.CreatedAt >= now);
        Assert.True(@event.ModifiedAt >= @event.CreatedAt);
    }

    // ── GetByIdAsync ──────────────────────────────────────────────────────────

    /// <summary>
    /// [AC] REQ-EVENT-03: Verify GetByIdAsync includes related QueueEntries and Venue.
    /// </summary>
    [Fact]
    public async Task GetByIdAsync_ExistingEvent_IncludesRelations()
    {
        var venue = await CreateVenueAsync("Test Venue");

        var @event = new Event
        {
            Name = "Karaoke Night",
            VenueId = venue.Id,
            Venue = venue,
            ScheduledStartTime = new DateTime(2026, 6, 15, 20, 0, 0)
        };

        _db.QueueManagementEvents.Add(@event);
        await _db.SaveChangesAsync();

        var found = await _repo.GetByIdAsync(@event.Id, CancellationToken.None);

        Assert.NotNull(found);
        Assert.NotNull(found.Venue);
        Assert.Equal("Test Venue", found.Venue.Name);
        Assert.NotNull(found.QueueEntries);
    }

    /// <summary>
    /// Verify GetByIdAsync returns null for non-existent ID.
    /// </summary>
    [Fact]
    public async Task GetByIdAsync_NonExistentId_ReturnsNull()
    {
        var found = await _repo.GetByIdAsync(999, CancellationToken.None);

        Assert.Null(found);
    }

    // ── GetPagedAsync ─────────────────────────────────────────────────────────

    /// <summary>
    /// [AC] REQ-EVENT-04: Verify GetPagedAsync returns all events ordered by ScheduledStartTime descending.
    /// </summary>
    [Fact]
    public async Task GetPagedAsync_NoQuery_ReturnsAllOrderedByScheduledStartTimeDesc()
    {
        var venue1 = await CreateVenueAsync("Venue 1");
        var venue2 = await CreateVenueAsync("Venue 2");

        var event1 = new Event
        {
            Name = "Early Event",
            VenueId = venue1.Id,
            Venue = venue1,
            ScheduledStartTime = new DateTime(2026, 6, 10, 20, 0, 0)
        };
        var event2 = new Event
        {
            Name = "Late Event",
            VenueId = venue2.Id,
            Venue = venue2,
            ScheduledStartTime = new DateTime(2026, 6, 20, 20, 0, 0)
        };

        _db.QueueManagementEvents.AddRange(event1, event2);
        await _db.SaveChangesAsync();

        var (items, totalCount) = await _repo.GetPagedAsync(1, 20, null, CancellationToken.None);

        Assert.Equal(2, totalCount);
        var list = items.ToList();
        Assert.Equal("Late Event", list[0].Name);       // descending by ScheduledStartTime
        Assert.Equal("Early Event", list[1].Name);
    }

    /// <summary>
    /// [AC] REQ-EVENT-05: Verify GetPagedAsync filters by name query (case-insensitive).
    /// </summary>
    [Fact]
    public async Task GetPagedAsync_WithQuery_FiltersResults()
    {
        var venue1 = await CreateVenueAsync("Venue 1");
        var venue2 = await CreateVenueAsync("Venue 2");

        var event1 = new Event
        {
            Name = "Rock Karaoke",
            VenueId = venue1.Id,
            Venue = venue1,
            ScheduledStartTime = new DateTime(2026, 6, 15, 20, 0, 0)
        };
        var event2 = new Event
        {
            Name = "Jazz Night",
            VenueId = venue2.Id,
            Venue = venue2,
            ScheduledStartTime = new DateTime(2026, 6, 16, 20, 0, 0)
        };

        _db.QueueManagementEvents.AddRange(event1, event2);
        await _db.SaveChangesAsync();

        var (items, totalCount) = await _repo.GetPagedAsync(1, 20, "rock", CancellationToken.None);

        Assert.Equal(1, totalCount);
        Assert.Equal("Rock Karaoke", items.Single().Name);
    }

    /// <summary>
    /// Verify GetPagedAsync supports pagination with skip/take.
    /// </summary>
    [Fact]
    public async Task GetPagedAsync_Page2_SkipsFirstPage()
    {
        var venue = await CreateVenueAsync("Test Venue");

        for (int i = 1; i <= 5; i++)
        {
            var @event = new Event
            {
                Name = $"Event {i:D2}",
                VenueId = venue.Id,
                Venue = venue,
                ScheduledStartTime = new DateTime(2026, 6, 10 + i, 20, 0, 0)
            };
            _db.QueueManagementEvents.Add(@event);
        }
        await _db.SaveChangesAsync();

        var (items, totalCount) = await _repo.GetPagedAsync(2, 2, null, CancellationToken.None);

        Assert.Equal(5, totalCount);
        Assert.Equal(2, items.Count());
    }

    /// <summary>
    /// Verify GetPagedAsync returns empty results for empty database.
    /// </summary>
    [Fact]
    public async Task GetPagedAsync_EmptyDb_ReturnsZeroTotal()
    {
        var (items, totalCount) = await _repo.GetPagedAsync(1, 20, null, CancellationToken.None);

        Assert.Equal(0, totalCount);
        Assert.Empty(items);
    }

    // ── UpdateAsync ───────────────────────────────────────────────────────────

    /// <summary>
    /// Verify UpdateAsync modifies event and updates ModifiedAt timestamp.
    /// </summary>
    [Fact]
    public async Task UpdateAsync_ExistingEvent_UpdatesAndTimestamp()
    {
        var venue = await CreateVenueAsync("Test Venue");

        var @event = new Event
        {
            Name = "Original Name",
            VenueId = venue.Id,
            Venue = venue,
            ScheduledStartTime = new DateTime(2026, 6, 15, 20, 0, 0)
        };

        _db.QueueManagementEvents.Add(@event);
        await _db.SaveChangesAsync();

        var originalModified = @event.ModifiedAt;
        await Task.Delay(10); // ensure time difference

        @event.Name = "Updated Name";
        await _repo.UpdateAsync(@event, CancellationToken.None);

        var updated = await _repo.GetByIdAsync(@event.Id, CancellationToken.None);
        Assert.Equal("Updated Name", updated!.Name);
        Assert.True(updated.ModifiedAt > originalModified);
    }

    // ── DeleteAsync ───────────────────────────────────────────────────────────

    /// <summary>
    /// Verify DeleteAsync removes event from database.
    /// </summary>
    [Fact]
    public async Task DeleteAsync_ExistingEvent_Removed()
    {
        var venue = await CreateVenueAsync("Test Venue");

        var @event = new Event
        {
            Name = "Event to Delete",
            VenueId = venue.Id,
            Venue = venue,
            ScheduledStartTime = new DateTime(2026, 6, 15, 20, 0, 0)
        };

        _db.QueueManagementEvents.Add(@event);
        await _db.SaveChangesAsync();

        await _repo.DeleteAsync(@event.Id, CancellationToken.None);

        var found = await _repo.GetByIdAsync(@event.Id, CancellationToken.None);
        Assert.Null(found);
    }

    // ── ExistsByNameAsync ─────────────────────────────────────────────────────

    /// <summary>
    /// [AC] REQ-EVENT-06: Verify ExistsByNameAsync returns true for existing name.
    /// </summary>
    [Fact]
    public async Task ExistsByNameAsync_ExistingName_ReturnsTrue()
    {
        var venue = await CreateVenueAsync("Test Venue");

        var @event = new Event
        {
            Name = "Karaoke Night",
            VenueId = venue.Id,
            Venue = venue,
            ScheduledStartTime = new DateTime(2026, 6, 15, 20, 0, 0)
        };

        _db.QueueManagementEvents.Add(@event);
        await _db.SaveChangesAsync();

        var exists = await _repo.ExistsByNameAsync("Karaoke Night", CancellationToken.None);

        Assert.True(exists);
    }

    /// <summary>
    /// Verify ExistsByNameAsync returns false for non-existent name.
    /// </summary>
    [Fact]
    public async Task ExistsByNameAsync_NonExistentName_ReturnsFalse()
    {
        var exists = await _repo.ExistsByNameAsync("Does Not Exist", CancellationToken.None);

        Assert.False(exists);
    }

    /// <summary>
    /// Verify ExistsByNameAsync is case-insensitive.
    /// </summary>
    [Fact]
    public async Task ExistsByNameAsync_CaseInsensitive_FindsMatch()
    {
        var venue = await CreateVenueAsync("Test Venue");

        var @event = new Event
        {
            Name = "Karaoke Night",
            VenueId = venue.Id,
            Venue = venue,
            ScheduledStartTime = new DateTime(2026, 6, 15, 20, 0, 0)
        };

        _db.QueueManagementEvents.Add(@event);
        await _db.SaveChangesAsync();

        var exists = await _repo.ExistsByNameAsync("karaoke night", CancellationToken.None);

        Assert.True(exists);
    }
}
