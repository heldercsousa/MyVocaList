using MyVocaList.Domain.ServicesInterfaces;
using MyVocaList.Tests.Infrastructure;

namespace MyVocaList.Tests.Integration.UnitOfWork;

/// <summary>
/// BUG-078 regression (Major severity ⇒ mandatory regression test, <c>.claude/rules/bug-tracking.md</c>).
/// <para>
/// <c>ArtistRepository.GetByIdAsync</c> is the repository layer's only <c>.AsTracking()</c> read, and
/// <c>ArtistService.GetDeleteConfirmationAsync</c> reaches it through the constructor-injected
/// (captive, session-lifetime) repository — outside any unit of work. The first delete-confirmation
/// attaches a tracked <c>Artist</c> to that captive context; a later rename commits through a
/// different, fresh context owned by <see cref="MyVocaList.Domain.UnitOfWork.IUnitOfWork"/>; the next
/// delete-confirmation is served from the captive context's change tracker and shows the OLD name.
/// It fails silently — nothing throws (this is not the BUG-068 tracking-conflict shape).
/// </para>
/// </summary>
public class Bug078RegressionTests
{
    // [AC] REQ-UOW-45: after a rename committed through IUnitOfWork, a subsequent
    // GetDeleteConfirmationAsync for the same artist id returns "Delete 'New Name'?" and never
    // the stale "Delete 'Old Name'?".
    [Fact]
    public async Task GetDeleteConfirmationAsync_AfterRenameCommittedThroughUnitOfWork_ShowsNewName()
    {
        await using var host = UnitOfWorkTestHost.Create();
        var artists = host.Resolve<IArtistService>();

        // Given an Artist row persisted with Name = "Old Name"
        var (createOk, createMessage, artist) = await artists.CreateArtistAsync("Old Name");
        Assert.True(createOk, createMessage);

        // And GetDeleteConfirmationAsync has already been called once (primes the captive read path)
        var firstConfirmation = await artists.GetDeleteConfirmationAsync([artist!.Id]);
        Assert.Equal("Delete 'Old Name'?", firstConfirmation);

        // When the artist is renamed through ArtistService.UpdateArtistAsync (commits via IUnitOfWork)
        var (updateOk, updateMessage) = await artists.UpdateArtistAsync(artist.Id, "New Name");
        Assert.True(updateOk, updateMessage);

        // And GetDeleteConfirmationAsync is called again
        var secondConfirmation = await artists.GetDeleteConfirmationAsync([artist.Id]);

        // Then the returned string is "Delete 'New Name'?" and not "Delete 'Old Name'?"
        Assert.Equal("Delete 'New Name'?", secondConfirmation);
    }
}
