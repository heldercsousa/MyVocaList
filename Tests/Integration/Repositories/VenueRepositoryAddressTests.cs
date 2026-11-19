using MyVocaList.Contracts.DTOs.Venues;
using MyVocaList.Domain.Entity;
using MyVocaList.Infra;
using MyVocaList.Infra.Repository;
using MyVocaList.Tests.Infrastructure;

namespace MyVocaList.Tests.Integration.Repositories;

/// <summary>
/// Name-and-address duplicate query (REQ-VEN-41 to 44) and local image references of <see cref="VenueRepository"/>
/// (REQ-VEN-04). Real SQLite temp file via <see cref="TestDbContextFactory"/>, never the in-memory provider.
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

    // ── NameAndAddressExistsAsync ────────────────────────────────────────────

    [Fact]
    public async Task NameAndAddressExistsAsync_SameNameAndSixFields_ReturnsTrue()
    {
        // [AC] REQ-VEN-41: another venue with an equal name and all six address fields equal is a duplicate
        await SeedVenueAsync("Jazz Club", RuaAugusta);

        var exists = await _repo.NameAndAddressExistsAsync("Jazz Club", RuaAugusta, excludeId: null);

        Assert.True(exists);
    }

    [Fact]
    public async Task NameAndAddressExistsAsync_DifferentLetterCaseAndAccents_ReturnsTrue()
    {
        // [AC] REQ-VEN-41: name and address equal under the database collation (case and accents ignored) are duplicates
        await SeedVenueAsync("Jazz Club", RuaAugusta);
        var typed = new VenueAddress("rua augusta, 100", null, "Sao Paulo", "sp", "01304-000", "brasil");

        var exists = await _repo.NameAndAddressExistsAsync("jazz club", typed, excludeId: null);

        Assert.True(exists);
    }

    [Fact]
    public async Task NameAndAddressExistsAsync_NullLine2OnBothSides_ReturnsTrue()
    {
        // [AC] REQ-VEN-41: a blank field equals only a blank field, so two null line 2 values with the other fields equal are a duplicate
        await SeedVenueAsync("Jazz Club", RuaAugusta with { Line2 = null });
        var searched = RuaAugusta with { Line2 = null };

        var exists = await _repo.NameAndAddressExistsAsync("Jazz Club", searched, excludeId: null);

        Assert.True(exists);
    }

    [Fact]
    public async Task NameAndAddressExistsAsync_SameNameOtherAddress_ReturnsFalse()
    {
        // [AC] REQ-VEN-42: the same name at a different address (City differs) is allowed
        await SeedVenueAsync("Jazz Club", RuaAugusta);
        var otherCity = RuaAugusta with { City = "Rio de Janeiro" };

        var exists = await _repo.NameAndAddressExistsAsync("Jazz Club", otherCity, excludeId: null);

        Assert.False(exists);
    }

    [Fact]
    public async Task NameAndAddressExistsAsync_SameNameOnlyLine2Differs_ReturnsFalse()
    {
        // [AC] REQ-VEN-42: the same name where only address line 2 differs ("Loja 2" vs none) is allowed
        await SeedVenueAsync("Jazz Club", RuaAugusta);
        var withLine2 = RuaAugusta with { Line2 = "Loja 2" };

        var exists = await _repo.NameAndAddressExistsAsync("Jazz Club", withLine2, excludeId: null);

        Assert.False(exists);
    }

    [Fact]
    public async Task NameAndAddressExistsAsync_SameNameStoredPostalCodeSearchedNull_ReturnsFalse()
    {
        // [AC] REQ-VEN-42: the same name where the stored postal code is searched as blank is allowed
        await SeedVenueAsync("Jazz Club", RuaAugusta);
        var withoutPostal = RuaAugusta with { PostalCode = null };

        var exists = await _repo.NameAndAddressExistsAsync("Jazz Club", withoutPostal, excludeId: null);

        Assert.False(exists);
    }

    [Fact]
    public async Task NameAndAddressExistsAsync_SameNameStoredWithAddressSearchedEmpty_ReturnsFalse()
    {
        // [AC] REQ-VEN-42: the same name with an empty address is allowed when the stored venue has an address
        await SeedVenueAsync("Jazz Club", RuaAugusta);

        var exists = await _repo.NameAndAddressExistsAsync("Jazz Club", VenueAddress.Empty, excludeId: null);

        Assert.False(exists);
    }

    [Fact]
    public async Task NameAndAddressExistsAsync_SameAddressOtherName_ReturnsFalse()
    {
        // [AC] REQ-VEN-43: a different name at the same six address fields is allowed
        await SeedVenueAsync("Jazz Club", RuaAugusta);

        var exists = await _repo.NameAndAddressExistsAsync("Blues Bar", RuaAugusta, excludeId: null);

        Assert.False(exists);
    }

    [Fact]
    public async Task NameAndAddressExistsAsync_SameNameBothAddressesEmpty_ReturnsTrue()
    {
        // [AC] REQ-VEN-44: two venues without an address cannot share a name (empty equals empty)
        await SeedVenueAsync("Jazz Club");

        var exists = await _repo.NameAndAddressExistsAsync("Jazz Club", VenueAddress.Empty, excludeId: null);

        Assert.True(exists);
    }

    [Fact]
    public async Task NameAndAddressExistsAsync_SameNameAddressless_OtherNameAddressless_ReturnsFalse()
    {
        // [AC] REQ-VEN-44: an empty address with a different name is not a duplicate
        await SeedVenueAsync("Jazz Club");

        var exists = await _repo.NameAndAddressExistsAsync("Rock Arena", VenueAddress.Empty, excludeId: null);

        Assert.False(exists);
    }

    [Fact]
    public async Task NameAndAddressExistsAsync_ExcludeIdOfTheOnlyMatch_ReturnsFalse()
    {
        // [AC] REQ-VEN-41: in edit mode the venue being edited is excluded, so its own name and address are not a duplicate
        var venue = await SeedVenueAsync("Jazz Club", RuaAugusta);

        var exists = await _repo.NameAndAddressExistsAsync("Jazz Club", RuaAugusta, excludeId: venue.Id);

        Assert.False(exists);
    }

    [Fact]
    public async Task NameAndAddressExistsAsync_ExcludeIdOfAnotherVenue_ReturnsTrue()
    {
        // [AC] REQ-VEN-41: excluding a different venue leaves the matching venue in the check
        await SeedVenueAsync("Jazz Club", RuaAugusta);
        var other = await SeedVenueAsync("Blues Bar");

        var exists = await _repo.NameAndAddressExistsAsync("Jazz Club", RuaAugusta, excludeId: other.Id);

        Assert.True(exists);
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
