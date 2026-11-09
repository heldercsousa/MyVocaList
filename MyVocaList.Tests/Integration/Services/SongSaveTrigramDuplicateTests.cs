using Microsoft.EntityFrameworkCore;
using MyVocaList.Domain.Resolution;
using MyVocaList.Infra;
using MyVocaList.Infra.Repository;
using MyVocaList.Infra.Similarity;
using MyVocaList.Tests.Infrastructure;

namespace MyVocaList.Tests.Integration.Services;

/// <summary>
/// BUG-085 save-level regression (REQ-SFT-23). Real SQLite (MigrateAsync, so SongSearch exists), real
/// <see cref="SongRepository"/>, real <see cref="SongResolutionService"/> and real
/// <see cref="SimilarityScorer"/>, with <see cref="SongService"/> on top. Only the artist-side
/// collaborators and the karaoke-URL collaborators are mocked; no write is expected.
/// </summary>
public class SongSaveTrigramDuplicateTests : IAsyncLifetime
{
    private AppDbContext _db;
    private SongRepository _songRepo;
    private Artist _queen;
    private Song _storedPatience;

    private readonly Mock<IArtistRepository> _artistRepo = new();
    private readonly Mock<ISongKaraokeUrlRepository> _urlRepo = new();
    private readonly Mock<ISongKaraokeUrlService> _urlService = new();
    private readonly Mock<IArtistService> _artistService = new();
    private readonly Mock<IArtistResolutionService> _artistResolution = new();
    private readonly Mock<ISongService> _songServiceForResolution = new();
    private readonly Mock<ILogger<SongService>> _songLogger = new();
    private readonly Mock<ILogger<SongResolutionService>> _resolutionLogger = new();

    public async Task InitializeAsync()
    {
        _db = TestDbContextFactory.Create();
        await _db.Database.MigrateAsync();
        _songRepo = new SongRepository(_db);

        _queen = new Artist { Name = "Queen", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        _db.Artists.Add(_queen);
        await _db.SaveChangesAsync();

        _storedPatience = new Song
        {
            ArtistId = _queen.Id,
            Title = "Patience",
            Version = "",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Songs.Add(_storedPatience);
        await _db.SaveChangesAsync();

        _artistRepo
            .Setup(r => r.GetByIdAsync(_queen.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_queen);
        // The artist is given by id, so artist resolution must not be needed; a NoMatch default keeps
        // the resolver's artist step inert if it is ever consulted.
        _artistResolution
            .Setup(a => a.ResolveAsync(It.IsAny<ArtistCandidate>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ArtistResolution(ResolutionKind.NoMatch, null, []));
    }

    public async Task DisposeAsync()
    {
        await _db.Database.EnsureDeletedAsync();
        await _db.DisposeAsync();
    }

    // [AC] REQ-SFT-23: saving "Patiencee" for an artist that stores "Patience" returns SongChoiceNeeded
    // (possible duplicates, not exact) listing the stored "Patience"; nothing is written.
    // [BUG] BUG-085
    [Fact]
    public async Task SaveFromFormAsync_LongerTypedTitle_ListsStoredNearDuplicate()
    {
        var outcome = await CreateSut().SaveFromFormAsync(new SongFormSaveRequest(
            null, "Patiencee", "", FeaturedArtists: null, Lyrics: null,
            _queen.Id, "Queen", null, null,
            PendingUrls: [],
            new ArtistDecision.None(),
            SongDecision.None));

        Assert.Equal(SongFormSaveKind.SongChoiceNeeded, outcome.Kind);
        Assert.False(outcome.IsExactSongMatch);
        var candidate = Assert.Single(outcome.SongCandidates);
        Assert.Equal(_storedPatience.Id, candidate.SongId);
        Assert.Equal("Patience", candidate.Title);
        Assert.Equal(1, await _db.Songs.CountAsync()); // only the seeded song; nothing saved
    }

    private SongService CreateSut()
    {
        // Resolution resolves its collaborators from its own unit-of-work scope (passthrough here).
        var resolution = new SongResolutionService(
            _songRepo,
            _artistResolution.Object,
            _songServiceForResolution.Object,
            new SimilarityScorer(),
            PassthroughUnitOfWork.Over(_songRepo, _artistResolution, _songServiceForResolution),
            _resolutionLogger.Object);

        var uow = PassthroughUnitOfWork.Over(
            _songRepo, _artistRepo, _urlRepo, _urlService, _artistService, _artistResolution, resolution);

        return new SongService(
            _songRepo,
            _artistRepo.Object,
            _urlRepo.Object,
            _urlService.Object,
            uow,
            _songLogger.Object);
    }
}
