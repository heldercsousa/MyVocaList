using Microsoft.EntityFrameworkCore;
using MyVocaList.Domain.Entities;
using MyVocaList.Infra;
using MyVocaList.Infra.Repositories;
using MyVocaList.Tests.Infrastructure;
using Event = MyVocaList.Domain.Entities.Event;
using QueueEntry = MyVocaList.Domain.Entities.QueueEntry;

namespace MyVocaList.Tests.Integration.Repositories;

/// <summary>
/// Integration tests for QueueRepository using real SQLite.
/// Tests CRUD operations, position management, reordering, and EF Core configuration.
/// </summary>
public class QueueRepositoryTests : IAsyncLifetime
{
    private AppDbContext _db = null!;
    private QueueRepository _repo = null!;

    public async Task InitializeAsync()
    {
        _db = TestDbContextFactory.Create();
        await _db.Database.EnsureCreatedAsync();
        _repo = new QueueRepository(_db);
    }

    public async Task DisposeAsync()
    {
        await _db.Database.EnsureDeletedAsync();
        await _db.DisposeAsync();
    }

    // ── GetByIdAsync ──────────────────────────────────────────────────────────

    /// <summary>
    /// Verify GetByIdAsync returns null for non-existent ID.
    /// </summary>
    [Fact]
    public async Task GetByIdAsync_NonExistentId_ReturnsNull()
    {
        var found = await _repo.GetByIdAsync(999, CancellationToken.None);

        Assert.Null(found);
    }

    // ── GetByEventIdAsync ─────────────────────────────────────────────────────

    /// <summary>
    /// Verify GetByEventIdAsync returns empty list for event with no queue entries.
    /// </summary>
    [Fact]
    public async Task GetByEventIdAsync_NoEntries_ReturnsEmpty()
    {
        var venue = new Venue { Name = "Test Venue" };
        _db.Set<Venue>().Add(venue);
        await _db.SaveChangesAsync();

        var @event = new Event
        {
            Name = "Test Event",
            VenueId = venue.Id,
            Venue = venue,
            ScheduledStartTime = new DateTime(2026, 6, 15, 20, 0, 0)
        };
        _db.QueueManagementEvents.Add(@event);
        await _db.SaveChangesAsync();

        var entries = await _repo.GetByEventIdAsync(@event.Id, CancellationToken.None);

        Assert.Empty(entries);
    }

    // ── DeleteAsync ───────────────────────────────────────────────────────────

    /// <summary>
    /// Verify DeleteAsync removes queue entry from database.
    /// </summary>
    [Fact]
    public async Task DeleteAsync_RemovesEntry()
    {
        // Create event, person, song first
        var venue = new Venue { Name = "Test Venue" };
        _db.Set<Venue>().Add(venue);
        await _db.SaveChangesAsync();

        var @event = new Event
        {
            Name = "Test Event",
            VenueId = venue.Id,
            Venue = venue,
            ScheduledStartTime = new DateTime(2026, 6, 15, 20, 0, 0)
        };
        _db.QueueManagementEvents.Add(@event);
        await _db.SaveChangesAsync();

        var person = new Person("Singer 1");
        _db.Set<Person>().Add(person);
        await _db.SaveChangesAsync();

        var artist = new Artist { Name = "Test Artist" };
        _db.Set<Artist>().Add(artist);
        await _db.SaveChangesAsync();

        var song = new Song { Title = "Test Song", ArtistId = artist.Id, OriginalArtist = artist };
        _db.Set<Song>().Add(song);
        await _db.SaveChangesAsync();

        // Create queue entry and add it
        var queueEntry = new QueueEntry
        {
            EventId = @event.Id,
            PersonId = person.Id,
            SongId = song.Id,
            Position = 1,
            Person = person,
            Event = @event,
            Song = song
        };

        _db.QueueEntries.Add(queueEntry);
        await _db.SaveChangesAsync();

        // Now delete it
        var entryId = queueEntry.Id;
        await _repo.DeleteAsync(entryId, CancellationToken.None);

        var found = await _repo.GetByIdAsync(entryId, CancellationToken.None);
        Assert.Null(found);
    }

    /// <summary>
    /// [AC] REQ-QUEUE-03: Verify GetByEventIdAsync returns entries ordered by position.
    /// </summary>
    [Fact]
    public async Task GetByEventIdAsync_ExistingEntries_ReturnsOrderedByPosition()
    {
        // Create event, person, song first
        var venue = new Venue { Name = "Test Venue" };
        _db.Set<Venue>().Add(venue);
        await _db.SaveChangesAsync();

        var @event = new Event
        {
            Name = "Test Event",
            VenueId = venue.Id,
            Venue = venue,
            ScheduledStartTime = new DateTime(2026, 6, 15, 20, 0, 0)
        };
        _db.QueueManagementEvents.Add(@event);
        await _db.SaveChangesAsync();

        var person1 = new Person("Singer 1");
        var person2 = new Person("Singer 2");
        _db.Set<Person>().AddRange(person1, person2);
        await _db.SaveChangesAsync();

        var artist = new Artist { Name = "Test Artist" };
        _db.Set<Artist>().Add(artist);
        await _db.SaveChangesAsync();

        var song = new Song { Title = "Test Song", ArtistId = artist.Id, OriginalArtist = artist };
        _db.Set<Song>().Add(song);
        await _db.SaveChangesAsync();

        // Create two queue entries
        var entry1 = new QueueEntry
        {
            EventId = @event.Id,
            PersonId = person1.Id,
            SongId = song.Id,
            Position = 1,
            Person = person1,
            Event = @event,
            Song = song
        };
        var entry2 = new QueueEntry
        {
            EventId = @event.Id,
            PersonId = person2.Id,
            SongId = song.Id,
            Position = 2,
            Person = person2,
            Event = @event,
            Song = song
        };

        _db.QueueEntries.AddRange(entry1, entry2);
        await _db.SaveChangesAsync();

        var entries = await _repo.GetByEventIdAsync(@event.Id, CancellationToken.None);

        Assert.Equal(2, entries.Count());
        var list = entries.ToList();
        Assert.Equal(1, list[0].Position);
        Assert.Equal(2, list[1].Position);
    }

    // ── ReorderAsync ──────────────────────────────────────────────────────────

    /// <summary>
    /// [AC] REQ-QUEUE-04: Verify ReorderAsync updates positions.
    /// </summary>
    [Fact]
    public async Task ReorderAsync_UpdatesPositions()
    {
        // Create event, persons, song first
        var venue = new Venue { Name = "Test Venue" };
        _db.Set<Venue>().Add(venue);
        await _db.SaveChangesAsync();

        var @event = new Event
        {
            Name = "Test Event",
            VenueId = venue.Id,
            Venue = venue,
            ScheduledStartTime = new DateTime(2026, 6, 15, 20, 0, 0)
        };
        _db.QueueManagementEvents.Add(@event);
        await _db.SaveChangesAsync();

        var person1 = new Person("Singer 1");
        var person2 = new Person("Singer 2");
        var person3 = new Person("Singer 3");
        _db.Set<Person>().AddRange(person1, person2, person3);
        await _db.SaveChangesAsync();

        var artist = new Artist { Name = "Test Artist" };
        _db.Set<Artist>().Add(artist);
        await _db.SaveChangesAsync();

        var song = new Song { Title = "Test Song", ArtistId = artist.Id, OriginalArtist = artist };
        _db.Set<Song>().Add(song);
        await _db.SaveChangesAsync();

        // Create three queue entries
        var entry1 = new QueueEntry
        {
            EventId = @event.Id,
            PersonId = person1.Id,
            SongId = song.Id,
            Position = 1,
            Person = person1,
            Event = @event,
            Song = song
        };
        var entry2 = new QueueEntry
        {
            EventId = @event.Id,
            PersonId = person2.Id,
            SongId = song.Id,
            Position = 2,
            Person = person2,
            Event = @event,
            Song = song
        };
        var entry3 = new QueueEntry
        {
            EventId = @event.Id,
            PersonId = person3.Id,
            SongId = song.Id,
            Position = 3,
            Person = person3,
            Event = @event,
            Song = song
        };

        _db.QueueEntries.AddRange(entry1, entry2, entry3);
        await _db.SaveChangesAsync();

        // Reorder: swap entries 1 and 3
        entry1.Position = 3;
        entry3.Position = 1;

        await _repo.ReorderAsync([entry1, entry3], CancellationToken.None);

        var reordered = await _repo.GetByEventIdAsync(@event.Id, CancellationToken.None);
        var list = reordered.ToList();
        Assert.Equal("Singer 3", list[0].Person.FullName); // now at position 1
        Assert.Equal("Singer 1", list[2].Person.FullName); // now at position 3
    }
}
