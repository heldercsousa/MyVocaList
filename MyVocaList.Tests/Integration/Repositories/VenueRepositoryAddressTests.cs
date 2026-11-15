using MyVocaList.Contracts.DTOs.Venues;
using MyVocaList.Domain.Entity;
using MyVocaList.Infra;
using MyVocaList.Infra.Repository;
using MyVocaList.Tests.Infrastructure;

namespace MyVocaList.Tests.Integration.Repositories;

/// <summary>
/// Duplicate-address query and local image references of <see cref="VenueRepository"/> (REQ-VEN-20, 21, 04).
/// Real SQLite temp file via <see cref="TestDbContextFactory"/>, never the in-memory provider.
/// </summary>
public class VenueRepositoryAddressTests : IAsyncLifetime
{
    private static readonly VenueAddress RuaAugusta =
        new("Rua Augusta, 100", null, "São Paulo", "SP", "01304-000", "Brasil");

    private AppDbContext _db = null!;
    private VenueRepository _repo = null!;

    public async Task InitializeAsync()
    {
        _db = TestDbContextFactory.Create();
        await _db.Database.EnsureCreatedAsync();
        _repo = new VenueRepository(_db);
    }

    public async Task DisposeAsync()
    {
        await _db.Database.EnsureDeletedAsync();
        await _db.DisposeAsync();
    }

    private async Task<Venue> SeedVenueAsync(string name, VenueAddress? address = null)
    {
        var venue = new Venue
        {
            Name = name,
            AddressLine1 = address?.Line1,
            AddressLine2 = address?.Line2,
            City = address?.City,
            Region = address?.Region,
            PostalCode = address?.PostalCode,
            Country = address?.Country,
        };
        _db.Venues.Add(venue);
        await _db.SaveChangesAsync();
        return venue;
    }

    // ── AddressExistsAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task AddressExistsAsync_SameSixFields_ReturnsTrue()
    {
        // [AC] REQ-VEN-20: another venue with all six address fields equal makes the address a duplicate
        await SeedVenueAsync("Bar Brahma", RuaAugusta);

        var exists = await _repo.AddressExistsAsync(RuaAugusta, excludeId: null);

        Assert.True(exists);
    }

    [Fact]
    public async Task AddressExistsAsync_DifferentLetterCaseAndAccents_ReturnsTrue()
    {
        // [AC] REQ-VEN-20: fields equal under the database collation NOCASE_NOACCENT (case and accents ignored) are duplicates
        await SeedVenueAsync("Bar Brahma", RuaAugusta);
        var typed = new VenueAddress("rua augusta, 100", null, "Sao Paulo", "sp", "01304-000", "brasil");

        var exists = await _repo.AddressExistsAsync(typed, excludeId: null);

        Assert.True(exists);
    }

    [Fact]
    public async Task AddressExistsAsync_OnlyLine2Differs_ReturnsFalse()
    {
        // [AC] REQ-VEN-20: an address line 2 that differs ("Loja 2" vs none) is not a duplicate
        await SeedVenueAsync("Bar Brahma", RuaAugusta);
        var withLine2 = RuaAugusta with { Line2 = "Loja 2" };

        var exists = await _repo.AddressExistsAsync(withLine2, excludeId: null);

        Assert.False(exists);
    }

    [Fact]
    public async Task AddressExistsAsync_NullLine2OnBothSides_ReturnsTrue()
    {
        // [AC] REQ-VEN-20: a blank field equals only a blank field, so two null line 2 values with the other five equal are a duplicate
        await SeedVenueAsync("Bar Brahma", RuaAugusta with { Line2 = null });
        var searched = RuaAugusta with { Line2 = null };

        var exists = await _repo.AddressExistsAsync(searched, excludeId: null);

        Assert.True(exists);
    }

    [Fact]
    public async Task AddressExistsAsync_DifferentCity_ReturnsFalse()
    {
        // [AC] REQ-VEN-20: a different city means the address is not a duplicate
        await SeedVenueAsync("Bar Brahma", RuaAugusta);
        var otherCity = RuaAugusta with { City = "Rio de Janeiro" };

        var exists = await _repo.AddressExistsAsync(otherCity, excludeId: null);

        Assert.False(exists);
    }

    [Fact]
    public async Task AddressExistsAsync_StoredPostalCodeSearchedNull_ReturnsFalse()
    {
        // [AC] REQ-VEN-20: a stored postal code does not equal a blank searched postal code
        await SeedVenueAsync("Bar Brahma", RuaAugusta);
        var withoutPostal = RuaAugusta with { PostalCode = null };

        var exists = await _repo.AddressExistsAsync(withoutPostal, excludeId: null);

        Assert.False(exists);
    }

    [Fact]
    public async Task AddressExistsAsync_ExcludeIdOfTheOnlyMatch_ReturnsFalse()
    {
        // [AC] REQ-VEN-20: in edit mode the venue being edited is excluded, so its own address is not a duplicate
        var venue = await SeedVenueAsync("Bar Brahma", RuaAugusta);

        var exists = await _repo.AddressExistsAsync(RuaAugusta, excludeId: venue.Id);

        Assert.False(exists);
    }

    [Fact]
    public async Task AddressExistsAsync_ExcludeIdOfAnotherVenue_ReturnsTrue()
    {
        // [AC] REQ-VEN-20: excluding a different venue leaves the matching venue in the check
        await SeedVenueAsync("Bar Brahma", RuaAugusta);
        var other = await SeedVenueAsync("Jazz Club");

        var exists = await _repo.AddressExistsAsync(RuaAugusta, excludeId: other.Id);

        Assert.True(exists);
    }

    [Fact]
    public async Task AddressExistsAsync_EmptyAddress_ReturnsFalseEvenWithEmptyVenuesStored()
    {
        // [AC] REQ-VEN-21: an empty address never collides, even when other venues also have empty addresses
        await SeedVenueAsync("Jazz Club");
        await SeedVenueAsync("Rock Arena");

        var exists = await _repo.AddressExistsAsync(VenueAddress.Empty, excludeId: null);

        Assert.False(exists);
    }

    // ── GetLocalImageReferencesAsync ─────────────────────────────────────────

    [Fact]
    public async Task GetLocalImageReferencesAsync_ReturnsOnlyLocalVenueReferences()
    {
        // [AC] REQ-VEN-04: the sweep reads the local venue references and skips null and https values
        const string local = "local:venues/0123456789abcdef0123456789abcdef";
        _db.Venues.AddRange(
            new Venue { Name = "Local", ImageUrl = local },
            new Venue { Name = "Remote", ImageUrl = "https://e.test/venue.jpg" },
            new Venue { Name = "None", ImageUrl = null });
        await _db.SaveChangesAsync();

        var result = await _repo.GetLocalImageReferencesAsync();

        Assert.Equal([local], result);
    }
}
