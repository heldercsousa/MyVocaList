using Microsoft.Extensions.DependencyInjection;
using MyVocaList.Contracts.DTOs;
using MyVocaList.Contracts.DTOs.List;
using MyVocaList.Contracts.Enums;
using MyVocaList.Domain.Entity;
using MyVocaList.Domain.RepositoryInterface;
using MyVocaList.Domain.Resolution;
using MyVocaList.Domain.ServicesInterfaces;
using MyVocaList.Domain.UnitOfWork;
using MyVocaList.Extensions.Strings;

namespace MyVocaList.Services;

public class SongService : ISongService
{
    private readonly ISongRepository _songRepository;
    private readonly IArtistRepository _artistRepository;
    private readonly ISongKaraokeUrlRepository _urlRepository;
    private readonly ISongKaraokeUrlService _urlService;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<SongService> _logger;

    public int MaxTitleLength => 250;
    public int ShowCounterAt => 80;

    /// <summary>Most near-duplicate artists offered on the artist-choice sheet (REQ-SFT-19).</summary>
    private const int MaxArtistChoiceCandidates = 3;

    /// <summary>Score reported for the stored song of an exact-duplicate outcome (REQ-SFT-22/25).</summary>
    private const double ExactSongMatchScore = 1.0;

    private const string ArtistRequiredMessage = "Artist is required";
    private const string ArtistNotFoundMessage = "Artist not found";
    private const string DeezerArtistFrozenMessage = "The artist of a song from the music database cannot be changed.";
    private const string DeezerTitleFrozenMessage = "The title of a song from the music database cannot be changed.";

    /// <summary>The one duplicate message for an exact song identity (REQ-RMV-06, REQ-RMV-10).</summary>
    private const string DuplicateTitleMessage = "This artist already has a song with this title.";

    public SongService(
        ISongRepository songRepository,
        IArtistRepository artistRepository,
        ISongKaraokeUrlRepository urlRepository,
        ISongKaraokeUrlService urlService,
        IUnitOfWork uow,
        ILogger<SongService> logger)
    {
        _songRepository = songRepository;
        _artistRepository = artistRepository;
        _urlRepository = urlRepository;
        _urlService = urlService;
        _uow = uow;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<string?> EnsureLyricsAsync(int songId, CancellationToken ct = default)
    {
        // Step 1 (read scope, REQ-SLD-19, 24): the preconditions, in order. Any failure returns null with no request.
        // The lyrics service is resolved here, after the preconditions, with the non-throwing GetService: a missing
        // registration means no lookup, and no other SongService path resolves it.
        var request = await _uow.ExecuteReadAsync<(LyricsQuery query, ISongLyricsService lyrics)?>(async sp =>
        {
            var song = await sp.GetRequiredService<ISongRepository>().GetByIdAsync(songId, ct);
            if (song is null
                || !MyVocaList.Domain.Rules.LyricsOriginRules.NeedsLookupOnOpen(
                    song.ExternalProvider, song.ExternalId, song.Lyrics, song.LyricsOrigin))
                return null;

            var title = song.Title;
            var artistName = song.OriginalArtist?.Name;
            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(artistName))
                return null;

            var lyricsService = sp.GetService<ISongLyricsService>();
            if (lyricsService is null)
                return null;

            return (new LyricsQuery(title, artistName), lyricsService);
        }, ct);

        if (request is not { } found)
            return null;

        // Step 2 (no unit of work open, REQ-SLD-20): the lookup may take seconds, so no scope, context or
        // transaction is held while it runs. The lyrics service and its provider hold nothing the scope disposes
        // (a network-status reader and an HTTP client from the factory), which is why the instance may be used
        // after the read scope has closed; the HTTP wait must not hold a scope open.
        // Caller cancellation propagates as OperationCanceledException.
        var answer = await found.lyrics.LookupLyricsAsync(found.query, ct);
        if (answer.Status == LyricsLookupStatus.NoAnswer)
            return null;

        // Step 3 (write scope, not cancellable: the answer is already in hand). The song is re-read; a change made
        // meanwhile (lyrics typed, origin recorded) wins and nothing is saved.
        try
        {
            var stored = await _uow.ExecuteAsync<(bool success, string message, string? lyrics)>(async sp =>
            {
                var songRepository = sp.GetRequiredService<ISongRepository>();
                var song = await songRepository.GetByIdAsync(songId, CancellationToken.None);
                if (song is null)
                    return (false, "Song not found", null);

                if (!string.IsNullOrWhiteSpace(song.Lyrics) || song.LyricsOrigin is not null)
                    return (false, string.Empty, song.Lyrics);

                // BUG-067 (see UpdateSongCoreAsync): the eager-loaded artist navigation is detached before the write.
                song.OriginalArtist = null;
                if (answer.Status == LyricsLookupStatus.Found)
                {
                    song.Lyrics = answer.Lyrics;
                    song.LyricsOrigin = LyricsOrigin.LyricsSource;
                }
                else
                {
                    song.LyricsOrigin = LyricsOrigin.NoneFound;
                }

                await songRepository.UpdateAsync(song, CancellationToken.None);
                return (true, string.Empty, answer.Status == LyricsLookupStatus.Found ? answer.Lyrics : null);
            }, CancellationToken.None);

            return stored.lyrics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lyrics found on open could not be stored for song {SongId}", songId);
            return null;
        }
    }

    /// <inheritdoc />
    public (bool isValid, string message) ValidateTitleInput(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return (false, "Song title is required");

        title = title.Trim();

        if (title.Length > MaxTitleLength)
            return (false, $"Title is too long. Maximum {MaxTitleLength} characters.");

        return (true, string.Empty);
    }

    /// <inheritdoc />
    public Task<(bool success, string message, Song? song)> CreateSongAsync(
        int artistId, string title, string? lyrics = null,
        string? externalId = null, string? externalProvider = null, CancellationToken ct = default)
        => _uow.ExecuteAsync<(bool success, string message, Song? song)>(async sp =>
        {
            // REQ-UOW-28: both repositories resolved from the lambda's own scope — never the
            // constructor fields, which hold the window-scope AppDbContext that caused BUG-068.
            var songRepository = sp.GetRequiredService<ISongRepository>();
            var artistRepository = sp.GetRequiredService<IArtistRepository>();

            var (isValid, message) = ValidateTitleInput(title);
            if (!isValid)
                return (false, message, null);

            var artist = await artistRepository.GetByIdAsync(artistId, ct);
            if (artist == null)
                return (false, "Artist not found", null);

            // Song.Title trimming is enforced by the EF Core ValueConverter configured in
            // SongConfiguration (design.md § D3) — not here. EF applies the same converter to this
            // WHERE parameter, so an untrimmed check value still matches trimmed stored rows.
            // REQ-RMV-06: identity is (Artist, Title) only, case- and accent-insensitive (collation).
            if (await songRepository.ExistsByTitleForArtistAsync(artistId, title, ct))
                return (false, DuplicateTitleMessage, null);

            // Song.Title trimming is enforced by the ValueConverter (design.md § D3).
            var song = new Song
            {
                ArtistId = artistId,
                Title = title,
                Lyrics = lyrics,
                LyricsOrigin = MyVocaList.Domain.Rules.LyricsOriginRules.OnCreate(lyrics, null),
                ExternalId = externalId,
                ExternalProvider = externalProvider,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await songRepository.AddAsync(song, ct);

            // REQ-CATPROP-01: the author performs their own song. Staged through the Song navigation so EF
            // fixes up SongId on the single save; resolved from sp (REQ-UOW-28), committed with the song.
            await sp.GetRequiredService<ICatalogRepository>()
                .AddAsync(new Catalog { Song = song, ArtistId = artistId }, ct);
            // SaveChangesAsync deleted — the single save is owned by IUnitOfWork (REQ-UOW-10).
            return (true, $"Song '{title.Trim()}' created successfully", song);
        }, ct);

    /// <inheritdoc />
    public async Task<(bool success, string message)> UpdateSongAsync(
        int id, string title, string? lyrics,
        string? externalId = null, string? externalProvider = null,
        int? artistId = null, string? newArtistName = null,
        CancellationToken ct = default)
    {
        var (success, message, _) = await UpdateSongCoreAsync(
            id, title, lyrics, externalId, externalProvider,
            artistId, newArtistName, ct);
        return (success, message);
    }

    /// <summary>
    /// The body of <see cref="UpdateSongAsync"/> (REQ-SFT-46: the only update write). It also returns the
    /// form field a failure belongs to (REQ-SFT-51, D2); only <see cref="SaveFromFormAsync"/> reads it.
    /// The trailing picture parameters are passed by <see cref="SaveFromFormAsync"/> only; their defaults
    /// keep <see cref="UpdateSongAsync"/> unchanged (it never touches a picture).
    /// </summary>
    /// <param name="imageChanged">REQ-PIC-21: true = <paramref name="songImageReference"/> replaces the stored reference.</param>
    /// <param name="songImageReference">The already-normalised song reference (null removes the picture).</param>
    /// <param name="artistImageUrl">The already-normalised pending artist picture (REQ-ART-16 in edit mode).</param>
    /// <param name="fillArtistImage">False for an artist chosen through <c>ArtistDecision.UseExisting</c>.</param>
    private Task<(bool success, string message, SaveField failedField)> UpdateSongCoreAsync(
        int id, string title, string? lyrics,
        string? externalId, string? externalProvider,
        int? artistId, string? newArtistName,
        CancellationToken ct,
        bool imageChanged = false, string? songImageReference = null,
        string? artistImageUrl = null, bool fillArtistImage = false)
    {
        // REQ-SFT-28 / REQ-SFT-21: at most one of artistId / newArtistName (blank name = not
        // supplied). Pure input validation stays OUTSIDE the lambda (REQ-UOW-41).
        var hasNewArtistName = !string.IsNullOrWhiteSpace(newArtistName);
        if (artistId.HasValue && hasNewArtistName)
            return Task.FromResult<(bool success, string message, SaveField failedField)>(
                (false, "Artist is required", SaveField.Artist));

        return _uow.ExecuteAsync<(bool success, string message, SaveField failedField)>(async sp =>
        {
            // REQ-UOW-28: resolved from the lambda's own scope — never the constructor field.
            var songRepository = sp.GetRequiredService<ISongRepository>();

            var (isValid, message) = ValidateTitleInput(title);
            if (!isValid)
                return (false, message, SaveField.Title);

            var song = await songRepository.GetByIdAsync(id, ct);
            if (song == null)
                return (false, "Song not found", SaveField.None);

            // REQ-SLD-15: the artist of a Deezer song cannot change, whatever the caller sends; checked before
            // the identity check and before any artist is created.
            if (MyVocaList.Domain.Rules.SongIdentityRules.IsDeezerSong(song.ExternalProvider, song.ExternalId)
                && (hasNewArtistName || (artistId.HasValue && artistId.Value != song.ArtistId)))
                return (false, DeezerArtistFrozenMessage, SaveField.Artist);

            // REQ-SLD-37: the title of a Deezer song cannot change either; compared after the trimming every
            // title gets (ValidateTitleInput above), ordinal, character by character.
            if (MyVocaList.Domain.Rules.SongIdentityRules.IsDeezerSong(song.ExternalProvider, song.ExternalId)
                && !string.Equals(title.Trim(), song.Title, StringComparison.Ordinal))
                return (false, DeezerTitleFrozenMessage, SaveField.Title);

            // REQ-SFT-26: an external identity held by ANOTHER song (self excluded) is rejected before
            // any artist is created, so a refused save never leaves a new artist behind.
            if (!string.IsNullOrWhiteSpace(externalId) && !string.IsNullOrWhiteSpace(externalProvider)
                && await songRepository.ExistsByExternalIdentityAsync(externalProvider, externalId, id, ct))
                return (false, "This music-database song is already in your library", SaveField.None);

            // BUG-067 (REQ-ACREATE-16): the artist FK used to be written on the create path only, so a
            // user changing the artist of a saved song lost the edit silently. null = keep existing
            // (same semantics as externalId below); a supplied id must exist.
            var effectiveArtistId = artistId ?? song.ArtistId;
            Artist? changedToExistingArtist = null;
            if (hasNewArtistName)
            {
                // REQ-SFT-28 / REQ-UOW-28: resolved from sp, so the nested service's ExecuteAsync JOINS
                // this ambient scope (one context, one transaction). A failure tuple is returned as-is
                // and the unit of work skips the save and rolls back.
                var artistService = sp.GetRequiredService<IArtistService>();
                var (artistCreated, artistMessage, createdArtist) =
                    await artistService.CreateArtistAsync(newArtistName!, ct: ct);
                if (!artistCreated || createdArtist is null)
                    return (false, artistMessage, SaveField.Artist);

                // REQ-ART-16 in edit mode (REQ-PIC-40): an artist created by this Save carries the pending
                // picture from its first flush (tracked Added entity, so no explicit UpdateAsync is needed).
                if (artistImageUrl is not null)
                    createdArtist.ImageUrl = artistImageUrl;

                // REQ-UOW-35: flush (not commit) so the generated Artist.Id exists; _uow holds no
                // AppDbContext (it flushes the AMBIENT scope), so REQ-UOW-28 does not apply to it.
                await _uow.FlushAsync(ct);
                effectiveArtistId = createdArtist.Id;
            }
            else if (artistId.HasValue && artistId.Value != song.ArtistId)
            {
                // REQ-UOW-28: resolved from the lambda's own scope — never the constructor field.
                var artistRepository = sp.GetRequiredService<IArtistRepository>();
                var newArtist = await artistRepository.GetByIdAsync(artistId.Value, ct);
                if (newArtist == null)
                    return (false, "Artist not found", SaveField.Artist);
                changedToExistingArtist = newArtist;
            }

            // REQ-RMV-06 / REQ-SFT-29 / REQ-SFT-27: identity = (collated effective artist, title) excluding
            // this song (REQ-RMV-06: Version no longer takes part). Title trimming is enforced by the EF
            // ValueConverter (design.md § D3), which also applies to this WHERE parameter. The check runs on
            // the EFFECTIVE artist, so it also refuses an artist change that would duplicate another song
            // (REQ-PIC-32); that refusal belongs to the Artist field.
            if (await songRepository.FindIdByTitleForArtistAsync(
                    effectiveArtistId, title, id, ct) is not null)
                return (false, DuplicateTitleMessage,
                    effectiveArtistId != song.ArtistId ? SaveField.Artist : SaveField.None);

            // REQ-ART-16 in edit mode (REQ-PIC-40): an existing artist the song is MOVED to is only ever
            // filled, never overwritten, and only after every refusal above (REQ-PIC-22). An unchanged
            // artist is never filled (pinned by SaveFromForm_EditModeWithImages_LeavesStoredImagesUnchanged).
            // Explicit UpdateAsync: GetByIdAsync returns the artist detached (NoTracking default), so
            // mutating it alone is a silent no-op at save time (artwork item T10a, commit 04f0f329).
            if (fillArtistImage && artistImageUrl is not null && changedToExistingArtist is { ImageUrl: null })
            {
                changedToExistingArtist.ImageUrl = artistImageUrl;
                await sp.GetRequiredService<IArtistRepository>().UpdateAsync(changedToExistingArtist, ct);
            }

            // Song.Title trimming is enforced by the ValueConverter (design.md § D3).
            // BUG-067 under the unit-of-work pattern: SongRepository.GetByIdAsync eager-loads
            // OriginalArtist (BUG-055). Inside a fresh unit-of-work context the entity is untracked,
            // so ISongRepository.UpdateAsync attaches the whole graph and EF's FK fixup rewrites
            // ArtistId back from the stale OriginalArtist navigation — silently discarding the
            // artist change. Detaching the navigation before the write leaves the scalar/FK values
            // as the only source of truth, which is exactly what the pre-UOW stopgap's
            // CurrentValues.SetValues achieved ("scalar/FK properties only, no navigation graph").
            song.OriginalArtist = null;
            var oldArtistId = song.ArtistId;
            song.ArtistId = effectiveArtistId;
            song.Title = title;
            song.LyricsOrigin = MyVocaList.Domain.Rules.LyricsOriginRules.OnUpdate(song.Lyrics, song.LyricsOrigin, lyrics);
            song.Lyrics = lyrics;
            song.UpdatedAt = DateTime.UtcNow;

            // M2: persist external identity when provided; null = keep existing value
            if (externalId != null)
                song.ExternalId = externalId;
            if (externalProvider != null)
                song.ExternalProvider = externalProvider;

            // REQ-PIC-21: the stored reference changes only when the form uploaded or removed a picture.
            if (imageChanged)
                song.ImageUrl = songImageReference;

            await songRepository.UpdateAsync(song, ct);

            // REQ-CATPROP-03: an author change moves the derived row, after every refusal above. A row the
            // new author already holds as a cover becomes the derived row (no duplicate). Both calls run on
            // the unit of work's connection, so a failed save rolls them back (REQ-CATPROP-04).
            if (effectiveArtistId != oldArtistId)
            {
                var catalogRepository = sp.GetRequiredService<ICatalogRepository>();
                await catalogRepository.RemoveAsync(oldArtistId, song.Id, ct);
                if (!await catalogRepository.ExistsAsync(effectiveArtistId, song.Id, ct))
                    await catalogRepository.AddAsync(new Catalog { ArtistId = effectiveArtistId, SongId = song.Id }, ct);
            }

            // SaveChangesAsync deleted — the single save is owned by IUnitOfWork (REQ-UOW-10).
            return (true, $"Song updated to '{title}'", SaveField.None);
        }, ct);
    }

    /// <inheritdoc />
    public async Task<(bool success, string message, Song? song)> CreateSongWithUrlsAsync(
        int? artistId, string? newArtistName, string title,
        string? lyrics, string? externalId, string? externalProvider,
        IEnumerable<string> urls, CancellationToken ct = default)
    {
        var (success, message, song, _) = await CreateSongWithUrlsCoreAsync(
            artistId, newArtistName, title, lyrics,
            externalId, externalProvider, urls, ct);
        return (success, message, song);
    }

    /// <summary>
    /// The body of <c>CreateSongWithUrlsAsync(int?, string?, …)</c> (REQ-SFT-46: the only create write).
    /// It also returns the form field a failure belongs to (REQ-SFT-51, D2): an artist refused while it
    /// is created is <see cref="SaveField.Artist"/>; a song-level refusal after that is
    /// <see cref="SaveField.None"/>. Only <see cref="SaveFromFormAsync"/> reads the field.
    /// </summary>
    private Task<(bool success, string message, Song? song, SaveField failedField)> CreateSongWithUrlsCoreAsync(
        int? artistId, string? newArtistName, string title,
        string? lyrics, string? externalId, string? externalProvider,
        IEnumerable<string> urls, CancellationToken ct,
        string? songImageUrl = null, string? artistImageUrl = null, bool fillArtistImage = false,
        LyricsLookupResult? pickLyricsLookup = null)
    {
        // REQ-SFT-21: exactly one of artistId / newArtistName. Pure input validation — it stays
        // OUTSIDE the lambda (REQ-UOW-41), so a rejected call opens no DI scope and writes nothing.
        var hasNewArtistName = !string.IsNullOrWhiteSpace(newArtistName);
        if (artistId.HasValue == hasNewArtistName)
            return Task.FromResult<(bool success, string message, Song? song, SaveField failedField)>(
                (false, "Artist is required", null, SaveField.Artist));

        return _uow.ExecuteAsync<(bool success, string message, Song? song, SaveField failedField)>(async sp =>
        {
            // REQ-UOW-28: all three repositories AND the nested URL service are resolved from the
            // lambda's own scope — never the constructor fields. All three repositories therefore
            // share ONE AppDbContext, which is what keeps REQ-UOW-07's single-save FK fixup valid.
            var songRepository = sp.GetRequiredService<ISongRepository>();
            var urlRepository = sp.GetRequiredService<ISongKaraokeUrlRepository>();
            var urlService = sp.GetRequiredService<ISongKaraokeUrlService>();

            var (isValid, message) = ValidateTitleInput(title);
            if (!isValid)
                return (false, message, null, SaveField.Title);

            // REQ-SFT-26: an external identity already held by another song is rejected BEFORE any
            // artist is created, so a refused save never leaves a new artist behind.
            if (!string.IsNullOrWhiteSpace(externalId) && !string.IsNullOrWhiteSpace(externalProvider)
                && await songRepository.ExistsByExternalIdentityAsync(externalProvider, externalId, null, ct))
                return (false, "This music-database song is already in your library", null, SaveField.None);

            int effectiveArtistId;
            Artist? existingArtist = null;
            if (hasNewArtistName)
            {
                // REQ-SFT-18 / REQ-UOW-28: the nested service is resolved from sp, so its own
                // ExecuteAsync JOINS this ambient scope — one AppDbContext, one transaction. A
                // failure tuple (e.g. the REQ-SFT-52 collated-duplicate refusal) is returned as-is
                // and the unit of work skips the save and rolls back.
                var artistService = sp.GetRequiredService<IArtistService>();
                var (artistCreated, artistMessage, newArtist) =
                    await artistService.CreateArtistAsync(newArtistName!, ct: ct);
                if (!artistCreated || newArtist is null)
                    return (false, artistMessage, null, SaveField.Artist);

                // REQ-ART-16: an artist created by this Save carries the pending picture from its first flush.
                if (artistImageUrl is not null)
                    newArtist.ImageUrl = artistImageUrl;

                // REQ-UOW-35: flush (not commit) so the generated Artist.Id exists before the song is
                // staged. The transaction stays open — any later failure rolls the artist back too.
                // _uow holds no AppDbContext (it flushes the AMBIENT scope), so REQ-UOW-28 does not
                // apply to it (same rationale as ArtistResolutionService.CommitAsync).
                await _uow.FlushAsync(ct);
                effectiveArtistId = newArtist.Id;
            }
            else
            {
                // REQ-UOW-28: resolved from the lambda's own scope — never the constructor field.
                var artistRepository = sp.GetRequiredService<IArtistRepository>();
                var artist = await artistRepository.GetByIdAsync(artistId!.Value, ct);
                if (artist == null)
                    return (false, "Artist not found", null, SaveField.Artist);
                existingArtist = artist;
                effectiveArtistId = artistId.Value;
            }

            // REQ-RMV-06: identity = (Artist, Title). Title trimming is enforced by the EF Core ValueConverter
            // configured in SongConfiguration (design.md § D3) — not here. EF applies the same converter to
            // this WHERE parameter, so an untrimmed check value still matches trimmed stored rows.
            if (await songRepository.ExistsByTitleForArtistAsync(effectiveArtistId, title, ct))
                return (false, DuplicateTitleMessage, null, SaveField.None);

            // REQ-ART-16 / REQ-ART-18: an existing artist is only ever filled, never overwritten, and only
            // once every refusal above has passed. Explicit UpdateAsync: GetByIdAsync returns the artist
            // detached (DbContext-wide QueryTrackingBehavior.NoTracking, commit b2afd095), so mutating it
            // alone is a silent no-op at save time (same pattern as PersonService/VenueService updates).
            if (fillArtistImage && artistImageUrl is not null && existingArtist is { ImageUrl: null })
            {
                existingArtist.ImageUrl = artistImageUrl;
                await sp.GetRequiredService<IArtistRepository>().UpdateAsync(existingArtist, ct);
            }

            // Song.Title trimming is enforced by the ValueConverter (design.md § D3).
            var song = new Song
            {
                ArtistId = effectiveArtistId,
                Title = title,
                Lyrics = lyrics,
                LyricsOrigin = MyVocaList.Domain.Rules.LyricsOriginRules.OnCreate(lyrics, pickLyricsLookup),
                ExternalId = externalId,
                ExternalProvider = externalProvider,
                ImageUrl = songImageUrl,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Stage the song entity (no SaveChangesAsync yet)
            await songRepository.AddAsync(song, ct);

            // REQ-CATPROP-01: the derived catalog row, staged with the song (the artist id is real here,
            // even for an artist created in this save: it was flushed above).
            await sp.GetRequiredService<ICatalogRepository>()
                .AddAsync(new Catalog { Song = song, ArtistId = effectiveArtistId }, ct);

            // Stage URL entities on the SAME context — N3: one SaveChangesAsync commits both atomically
            var urlList = urls?.ToList() ?? [];
            foreach (var rawUrl in urlList)
            {
                var videoId = urlService.ExtractVideoId(rawUrl);
                if (videoId is null)
                {
                    _logger.LogWarning("Skipping invalid YouTube URL during atomic song create: {Url}", rawUrl);
                    continue;
                }

                var urlEntity = new SongKaraokeUrl
                {
                    // SongId is set by EF Core via the navigation after SaveChangesAsync resolves
                    // the song's generated PK. We set the reference via Song navigation property.
                    Song = song,
                    VideoId = videoId,
                    AddedAt = DateTime.UtcNow
                };

                await urlRepository.AddAsync(urlEntity, ct);
            }

            // REQ-UOW-07: the single commit — song + all URLs — is now owned by IUnitOfWork, which
            // saves the one shared context inside its explicit transaction (AC-6.2 preserved).
            return (true, $"Song '{title}' created successfully", song, SaveField.None);
        }, ct);
    }

    /// <inheritdoc />
    public async Task<SongFormSaveOutcome> SaveFromFormAsync(
        SongFormSaveRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // REQ-ART-03 / REQ-PIC-02: both pending images are normalised once, up front; an invalid value becomes
        // null. The song picture may be an image URL or a local reference (an upload); the artist picture stays
        // an image URL only, because the Song form never uploads an artist picture (design § 1 P19).
        var songImageUrl = MyVocaList.Domain.Rules.ImageReferenceRules.Normalize(request.ImageUrl);
        var artistImageUrl = MyVocaList.Domain.Rules.ImageUrlRules.Normalize(request.ArtistImageUrl);

        // Rule 1 (REQ-SFT-15 / REQ-SFT-12 / REQ-SFT-21): pure input validation — it stays OUTSIDE any
        // unit of work (REQ-UOW-41), so a rejected save opens no DI scope. The typed-artist-name rule
        // (REQ-SFT-20) needs IArtistService, so it runs first thing inside the read below.
        var (titleValid, titleMessage) = ValidateTitleInput(request.Title);
        if (!titleValid)
            return SaveFailed(titleMessage, SaveField.Title);

        if (request.ArtistId is null && string.IsNullOrWhiteSpace(request.ArtistText))
            return SaveFailed(ArtistRequiredMessage, SaveField.Artist);

        var isAddMode = request.SongId is null;

        // Rules 2–3 are reads only. SaveFromFormAsync opens no write unit of work (REQ-SFT-46): the
        // only writes are CreateSongWithUrlsAsync / UpdateSongAsync below, each with its own unit of
        // work. Resolving the governed services from sp (REQ-UOW-37) also keeps SongService free of
        // the SongResolutionService -> ISongService constructor cycle.
        var resolved = await _uow.ExecuteReadAsync<SaveFormResolution>(async sp =>
        {
            // REQ-SLD-15 / REQ-SLD-30: in edit mode the artist of a Deezer song is the stored one. The id is
            // compared, never the typed text; a match needs no artist resolution at all.
            if (!isAddMode)
            {
                var songRepository = sp.GetRequiredService<ISongRepository>();
                var storedSong = await songRepository.GetByIdAsync(request.SongId!.Value, ct);
                if (storedSong is not null
                    && MyVocaList.Domain.Rules.SongIdentityRules.IsDeezerSong(storedSong.ExternalProvider, storedSong.ExternalId))
                {
                    if (request.ArtistId != storedSong.ArtistId)
                        return SaveFormResolution.Stop(SaveFailed(DeezerArtistFrozenMessage, SaveField.Artist));

                    // REQ-SLD-37: the title of a Deezer song cannot change either; the trimmed request title is
                    // compared ordinally with the stored one (the same rule as UpdateSongCoreAsync).
                    if (!string.Equals(request.Title.Trim(), storedSong.Title, StringComparison.Ordinal))
                        return SaveFormResolution.Stop(SaveFailed(DeezerTitleFrozenMessage, SaveField.Title));

                    return SaveFormResolution.Proceed(storedSong.ArtistId, null, fillArtistImage: false);
                }
            }

            var artistStep = await ResolveFormArtistAsync(sp, request, ct);
            if (artistStep.StopOutcome is not null)
                return artistStep;

            // Rule 3 (REQ-SFT-22/23/25): add mode only, and only until the user has answered the
            // duplicate-song sheet. Edit mode never runs the guard (REQ-SFT-27).
            if (!isAddMode || request.SongDecision != SongDecision.None)
                return artistStep;

            var guardOutcome = await RunSongGuardAsync(sp, request, artistStep, ct);
            return guardOutcome is null ? artistStep : SaveFormResolution.Stop(guardOutcome);
        }, ct);

        if (resolved.StopOutcome is { } stopOutcome)
            return stopOutcome;

        // Rule 5 (REQ-SFT-46 / REQ-SFT-51): write through the two write paths only (their shared bodies,
        // see CreateSongWithUrlsCoreAsync / UpdateSongCoreAsync); a failure comes back as Failed with the
        // message unchanged and the field the failing step belongs to (D2).
        if (isAddMode)
        {
            // REQ-SFT-39 (ruling D1, 2026-11-05): an explicit SaveAsNewSong writes the new
            // row with a NULL external identity, so the identity refusal (REQ-SFT-26) cannot apply to it.
            // Nulled here, BEFORE the write, because the write-time check runs on whatever identity it gets.
            // With no decision (SongDecision.None) the identity is passed through and the refusal stands.
            var explicitSaveAsNew = request.SongDecision == SongDecision.SaveAsNewSong;
            var externalId = explicitSaveAsNew ? null : request.ExternalId;
            var externalProvider = explicitSaveAsNew ? null : request.ExternalProvider;

            var (created, createMessage, song, failedField) = await CreateSongWithUrlsCoreAsync(
                resolved.ArtistId, resolved.NewArtistName, request.Title,
                request.Lyrics, externalId, externalProvider,
                request.PendingUrls ?? [], ct,
                songImageUrl, artistImageUrl, resolved.FillArtistImage, request.PickLyricsLookup);

            return created && song is not null
                ? SaveSucceeded(createMessage, song.Id)
                : SaveFailed(createMessage, failedField);
        }

        // REQ-SFT-27: edit mode passes a null external identity — the stored identity is kept,
        // whatever the request carries. REQ-PIC-21: the song picture is written only when the form changed
        // it; REQ-PIC-40: the pending artist picture follows the REQ-ART-16 rule inside the same write.
        var songId = request.SongId!.Value;
        var (updated, updateMessage, updateFailedField) = await UpdateSongCoreAsync(
            songId, request.Title, request.Lyrics,
            externalId: null, externalProvider: null,
            artistId: resolved.ArtistId, newArtistName: resolved.NewArtistName, ct: ct,
            imageChanged: request.ImageChanged, songImageReference: songImageUrl,
            artistImageUrl: artistImageUrl, fillArtistImage: resolved.FillArtistImage);

        return updated
            ? SaveSucceeded(updateMessage, songId)
            : SaveFailed(updateMessage, updateFailedField);
    }

    /// <summary>
    /// Rule 2 (REQ-SFT-18/19/20/41/52): decides which artist the song is saved with. A picked id or a
    /// "Use existing" decision is used as is; a "Create" decision creates the typed name; otherwise
    /// the typed name — never the song's external identity (REQ-SFT-41) — goes to
    /// <see cref="IArtistResolutionService"/>, whose DB collation alone decides a match (REQ-SFT-52).
    /// </summary>
    private async Task<SaveFormResolution> ResolveFormArtistAsync(
        IServiceProvider sp, SongFormSaveRequest request, CancellationToken ct)
    {
        if (request.ArtistId is { } pickedArtistId)
            return SaveFormResolution.Proceed(pickedArtistId, newArtistName: null);

        if (request.ArtistDecision is ArtistDecision.UseExisting useExisting)
            return SaveFormResolution.Proceed(useExisting.ArtistId, newArtistName: null, fillArtistImage: false);

        // Rule 1, last check (REQ-SFT-20): the artist may be created, so the typed name is validated
        // with IArtistService's own rules before any resolution or write — never duplicated here.
        var artistService = sp.GetRequiredService<IArtistService>();
        var (nameValid, nameMessage) = artistService.ValidateNameInput(request.ArtistText!);
        if (!nameValid)
            return SaveFormResolution.Stop(SaveFailed(nameMessage, SaveField.Artist));

        // Trim only — case and accents are left to the DB collation (constraints-registry HARD RULE).
        var typedName = request.ArtistText!.Trim();

        if (request.ArtistDecision is ArtistDecision.CreateTyped)
            return SaveFormResolution.Proceed(artistId: null, typedName);

        var artistResolutionService = sp.GetRequiredService<IArtistResolutionService>();
        var resolution = await artistResolutionService.ResolveAsync(
            new ArtistCandidate(typedName, ExternalProvider: null, ExternalId: null), ct);

        switch (resolution.Kind)
        {
            case ResolutionKind.ExactLocalMatch or ResolutionKind.ExactExternalMatch
                when resolution.ExactMatchArtistId is { } exactArtistId:
                // REQ-SFT-18.1 / REQ-SFT-52: an exact collated match is reused silently.
                return SaveFormResolution.Proceed(exactArtistId, newArtistName: null);

            case ResolutionKind.FuzzyCandidates when resolution.FuzzyCandidates.Count > 0:
                var candidates = resolution.FuzzyCandidates
                    .OrderByDescending(c => c.Score)
                    .Take(MaxArtistChoiceCandidates)
                    .Select(c => new ArtistMatch(c.ArtistId, c.Name, c.Score))
                    .ToList();
                _logger.LogDebug("Song form save: artist '{Name}' has {Count} near-duplicate(s); choice needed",
                    typedName, candidates.Count);
                return SaveFormResolution.Stop(new SongFormSaveOutcome(
                    SongFormSaveKind.ArtistChoiceNeeded, string.Empty, SaveField.None, null,
                    candidates, false, [])
                {
                    TypedName = typedName
                });

            default:
                // NoMatch: the artist is created inside the song's write (REQ-SFT-18). Should a
                // collated duplicate exist after all, IArtistService refuses it (REQ-SFT-52).
                return SaveFormResolution.Proceed(artistId: null, typedName);
        }
    }

    /// <summary>
    /// Rule 3 (REQ-SFT-22/23/25): the add-mode duplicate-song guard. The external-identity tier runs
    /// whatever the artist's origin; the artist-scoped tiers run only when the artist already exists,
    /// which the resolver decides from the artist name: the typed name for a new artist, the STORED
    /// name for an existing one (<see cref="SongCandidate"/> carries no artist id). Returns
    /// <c>null</c> when nothing matches.
    /// </summary>
    private async Task<SongFormSaveOutcome?> RunSongGuardAsync(
        IServiceProvider sp, SongFormSaveRequest request, SaveFormResolution artistStep, CancellationToken ct)
    {
        string guardArtistName;
        if (artistStep.ArtistId is { } existingArtistId)
        {
            var artistRepository = sp.GetRequiredService<IArtistRepository>();
            var storedArtist = await artistRepository.GetByIdAsync(existingArtistId, ct);
            if (storedArtist is null)
                return SaveFailed(ArtistNotFoundMessage, SaveField.Artist);
            guardArtistName = storedArtist.Name;
        }
        else
        {
            guardArtistName = artistStep.NewArtistName!;
        }

        // REQ-SFT-41: the song's identity goes on the SongCandidate, never on its Artist.
        var songResolutionService = sp.GetRequiredService<ISongResolutionService>();
        var guard = await songResolutionService.ResolveAsync(
            new SongCandidate(
                request.Title, request.Lyrics,
                new ArtistCandidate(guardArtistName, ExternalProvider: null, ExternalId: null),
                request.ExternalProvider, request.ExternalId),
            ct);

        switch (guard.Kind)
        {
            case ResolutionKind.ExactLocalMatch or ResolutionKind.ExactExternalMatch
                when guard.ExactMatchSongId is { } exactSongId:
                var songRepository = sp.GetRequiredService<ISongRepository>();
                var existing = await songRepository.GetByIdAsync(exactSongId, ct);
                if (existing is null)
                {
                    // Same read scope as the resolver, so this needs a delete in between; the song no
                    // longer exists, so there is nothing to duplicate — the write's checks decide.
                    _logger.LogWarning("Song form save: exact duplicate {SongId} vanished before it was loaded",
                        exactSongId);
                    return null;
                }

                _logger.LogDebug("Song form save: exact duplicate song {SongId}; choice needed", exactSongId);
                return new SongFormSaveOutcome(
                    SongFormSaveKind.SongChoiceNeeded, string.Empty, SaveField.None, null, [], true,
                    [new SongMatch(existing.Id, existing.Title,
                        existing.OriginalArtist?.Name ?? guardArtistName, ExactSongMatchScore)]);

            case ResolutionKind.FuzzyCandidates when guard.FuzzyCandidates.Count > 0:
                _logger.LogDebug("Song form save: {Count} possible duplicate song(s); choice needed",
                    guard.FuzzyCandidates.Count);
                return new SongFormSaveOutcome(
                    SongFormSaveKind.SongChoiceNeeded, string.Empty, SaveField.None, null, [], false,
                    guard.FuzzyCandidates);

            default:
                return null;
        }
    }

    private static SongFormSaveOutcome SaveFailed(string message, SaveField field)
        => new(SongFormSaveKind.Failed, message, field, null, [], false, []);

    private static SongFormSaveOutcome SaveSucceeded(string message, int songId)
        => new(SongFormSaveKind.Saved, message, SaveField.None, songId, [], false, []);

    /// <summary>
    /// Intermediate result of the <see cref="SaveFromFormAsync"/> read: either an outcome that ends the
    /// save (<see cref="StopOutcome"/>), or the artist to write with — an existing id XOR a new name.
    /// </summary>
    private sealed record SaveFormResolution(
        SongFormSaveOutcome? StopOutcome, int? ArtistId, string? NewArtistName, bool FillArtistImage)
    {
        public static SaveFormResolution Stop(SongFormSaveOutcome outcome) => new(outcome, null, null, false);

        /// <param name="fillArtistImage">
        /// REQ-ART-16: false only for an artist chosen through <c>ArtistDecision.UseExisting</c>.
        /// </param>
        public static SaveFormResolution Proceed(int? artistId, string? newArtistName, bool fillArtistImage = true)
            => new(null, artistId, newArtistName, fillArtistImage);
    }

    /// <inheritdoc />
    public Task<Song?> GetSongByIdAsync(int id, CancellationToken ct = default)
        => _uow.ExecuteReadAsync<Song?>(async sp =>
        {
            // REQ-UOW-37: resolved from the lambda's own scope — never the constructor field.
            var songRepository = sp.GetRequiredService<ISongRepository>();
            return await songRepository.GetByIdAsync(id, ct);
        }, ct);

    /// <inheritdoc />
    public Task<bool> ExistsByTitleForArtistAsync(
        string title, int artistId, int? excludeId = null, CancellationToken ct = default)
    {
        var trimmed = title.NormalizeSearchQuery();
        // REQ-UOW-41: the excludeId branch decision stays OUTSIDE the lambda — it is pure input
        // shaping, not a database call, and must not create an extra DI scope.
        return _uow.ExecuteReadAsync<bool>(async sp =>
        {
            // REQ-UOW-37: resolved from the lambda's own scope — never the constructor field.
            var songRepository = sp.GetRequiredService<ISongRepository>();
            return excludeId == null
                ? await songRepository.ExistsByTitleForArtistAsync(artistId, trimmed, ct)
                : await songRepository.ExistsByTitleForArtistAsync(artistId, trimmed, excludeId.Value, ct);
        }, ct);
    }

    /// <inheritdoc />
    public Task<(bool success, string message)> DeleteSongsAsync(
        IEnumerable<int> ids, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(ids);

        return _uow.ExecuteAsync<(bool success, string message)>(async sp =>
        {
            // REQ-UOW-28: the repository is resolved from the lambda's own scope — never the
            // constructor field, which holds the window-scope AppDbContext that caused BUG-068.
            // ISongRepository.DeleteAsync is ExecuteDeleteAsync-based (REQ-UOW-33), so the explicit
            // transaction opened by IUnitOfWork is what brings the bulk delete under the unit of work.
            var songRepository = sp.GetRequiredService<ISongRepository>();

            var idList = ids.ToList();
            if (idList.Count == 0)
                return (false, "No song selected for deletion");

            await songRepository.DeleteAsync(idList, ct);
            return (true, idList.Count == 1 ? "Song deleted" : $"{idList.Count} songs deleted");
        }, ct);
    }

    /// <inheritdoc />
    public Task<(IEnumerable<SongListItemDto> items, int totalCount)> GetPagedSongsForListAsync(
        int pageNumber, int pageSize, string? query = null, CancellationToken ct = default)
    {
        // REQ-UOW-41: argument validation and query normalization stay OUTSIDE the lambda — they
        // make no database call and must not create a DI scope.
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageNumber);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageSize);

        var normalizedQuery = string.IsNullOrWhiteSpace(query) ? null : query.NormalizeSearchQuery();

        return _uow.ExecuteReadAsync<(IEnumerable<SongListItemDto> items, int totalCount)>(async sp =>
        {
            // REQ-UOW-37: resolved from the lambda's own scope — never the constructor field.
            var songRepository = sp.GetRequiredService<ISongRepository>();
            var (items, totalCount) = await songRepository.GetPagedAsync(pageNumber, pageSize, normalizedQuery, ct);
            return (items, totalCount);
        }, ct);
    }

    /// <inheritdoc />
    public bool ShouldShowCharacterCounter(int currentLength) => currentLength > ShowCounterAt;

    /// <inheritdoc />
    public (string text, bool isWarning, bool isError) GetCharacterCounterInfo(int currentLength)
    {
        var text = $"{currentLength}/{MaxTitleLength}";
        var isWarning = currentLength > 90;
        // Error only when ValidateTitleInput would reject the same length (counter threshold
        // alignment, Form Validation Standard) — exactly MaxTitleLength is still valid.
        var isError = currentLength > MaxTitleLength;
        return (text, isWarning, isError);
    }
}
