using MyVocaList.Contracts.DTOs;
using MyVocaList.Domain.Rules;
using MyVocaList.Domain.RepositoryInterface;
using MyVocaList.Domain.ServicesInterfaces;
using MyVocaList.Domain.UnitOfWork;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace MyVocaList.Services;

/// <summary>
/// Resolves artist photo + credit, description and discography at runtime (REQ-MSRC-08, -09, -10):
/// MusicBrainz artist -> Wikidata relation -> P18 + description -> Commons image info. Nothing is persisted.
/// </summary>
public sealed partial class ArtistProfileService : IArtistProfileService
{
    private const string MusicBrainzBase = "https://musicbrainz.org/ws/2/";
    private const string WikidataApi = "https://www.wikidata.org/w/api.php";
    private const string CommonsApi = "https://commons.wikimedia.org/w/api.php";
    private const int ThumbnailWidth = 500;
    private static readonly string[] DescriptionLanguages = ["pt", "en"];

    private readonly HttpClient _httpClient;
    private readonly MusicBrainzRateLimiter _rateLimiter;
    // Governed dependency kept for the REQ-UOW-36/37 convention; never dereferenced - repositories come from the scope's `sp`.
    private readonly IArtistRepository _artistRepository;
    private readonly IUnitOfWork _uow;
    private readonly IImageStore _imageStore;
    private readonly ILogger<ArtistProfileService> _logger;

    public ArtistProfileService(
        HttpClient httpClient, MusicBrainzRateLimiter rateLimiter,
        IArtistRepository artistRepository, IUnitOfWork uow, IImageStore imageStore, ILogger<ArtistProfileService> logger)
    {
        _httpClient = httpClient;
        _rateLimiter = rateLimiter;
        _artistRepository = artistRepository;
        _uow = uow;
        _imageStore = imageStore;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ArtistProfileDto?> GetAsync(string mbid, CancellationToken ct = default)
    {
        if (!Guid.TryParse(mbid, out var id))
            return null;

        try
        {
            await _rateLimiter.WaitAsync(ct);
            using var mb = await GetJsonAsync($"{MusicBrainzBase}artist/{id:D}?inc=url-rels+release-groups&fmt=json", ct);
            var discography = ReadDiscography(mb.RootElement);
            var qid = ReadWikidataId(mb.RootElement);

            string? description = null, imageUrl = null, credit = null, licenseUrl = null;
            if (qid is not null)
            {
                using var wd = await GetJsonAsync(
                    $"{WikidataApi}?action=wbgetentities&ids={qid}&props=claims%7Cdescriptions&languages=pt%7Cen&format=json", ct);
                if (wd.RootElement.TryGetProperty("entities", out var entities) && entities.TryGetProperty(qid, out var entity))
                {
                    description = ReadDescription(entity);
                    var fileName = ReadP18(entity);
                    if (fileName is not null)
                        (imageUrl, credit, licenseUrl) = await ResolveCommonsImageAsync(fileName, ct);
                }
            }

            return new ArtistProfileDto(imageUrl, credit, licenseUrl, description, discography);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or OperationCanceledException)
        {
            _logger.LogWarning(ex, "Artist profile lookup failed for {Mbid}", mbid);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<string?> AdoptPhotoIfMissingAsync(int artistId, ArtistProfileDto profile, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(profile.ImageUrl))
            return null;

        try
        {
            if (!await ArtistNeedsPictureAsync(artistId, ct))
                return null;   // REQ-MSRC-15: a picture the user set is never replaced

            await using var photo = new MemoryStream(await DownloadAsync(profile.ImageUrl, ct));
            var (imported, _, reference) = await _imageStore.ImportAsync(photo, ImageOwnerKind.Artist, ct);
            if (!imported || reference is null)
                return null;

            // Re-checked inside the write scope: a picture set while the download ran still wins.
            var adopted = await _uow.ExecuteAsync<bool>(async sp =>
            {
                var repo = sp.GetRequiredService<IArtistRepository>();
                var artist = await repo.GetByIdAsync(artistId, ct);
                if (artist is null || !string.IsNullOrWhiteSpace(artist.ImageUrl))
                    return false;
                artist.ImageUrl = ImageReferenceRules.Normalize(reference);
                artist.UpdatedAt = DateTime.UtcNow;
                await repo.UpdateAsync(artist, ct);
                return artist.ImageUrl is not null;
            }, ct);
            return adopted ? reference : null;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or IOException)
        {
            _logger.LogWarning(ex, "Artist photo adoption failed for artist {ArtistId}", artistId);
            return null;
        }
    }

    private Task<bool> ArtistNeedsPictureAsync(int artistId, CancellationToken ct) =>
        _uow.ExecuteReadAsync<bool>(async sp =>
        {
            var artist = await sp.GetRequiredService<IArtistRepository>().GetByIdAsync(artistId, ct);
            return artist is not null && string.IsNullOrWhiteSpace(artist.ImageUrl);
        }, ct);

    private async Task<byte[]> DownloadAsync(string url, CancellationToken ct)
    {
        using var response = await _httpClient.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync(ct);
    }

    private async Task<(string? Url, string? Credit, string? LicenseUrl)> ResolveCommonsImageAsync(string fileName, CancellationToken ct)
    {
        var title = Uri.EscapeDataString("File:" + fileName);
        using var doc = await GetJsonAsync(
            $"{CommonsApi}?action=query&titles={title}&prop=imageinfo&iiprop=url%7Cextmetadata&iiurlwidth={ThumbnailWidth}&format=json", ct);

        if (!doc.RootElement.TryGetProperty("query", out var query) || !query.TryGetProperty("pages", out var pages))
            return default;
        foreach (var page in pages.EnumerateObject())
        {
            if (!page.Value.TryGetProperty("imageinfo", out var infos) || infos.GetArrayLength() == 0)
                continue;
            var info = infos[0];
            var meta = info.TryGetProperty("extmetadata", out var m) ? m : default;
            var license = MetaValue(meta, "LicenseShortName");
            if (string.IsNullOrWhiteSpace(license))
                return default;   // REQ-MSRC-08: no licence, no photo

            var url = Str(info, "thumburl") ?? Str(info, "url");
            if (url is null)
                return default;
            var author = StripHtml(MetaValue(meta, "Artist"));
            var credit = string.IsNullOrWhiteSpace(author) ? license : $"{author}, {license}";
            return (url, credit, MetaValue(meta, "LicenseUrl"));
        }
        return default;
    }

    private async Task<JsonDocument> GetJsonAsync(string url, CancellationToken ct)
    {
        using var response = await _httpClient.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();
        return await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
    }

    private static List<ReleaseGroupDto> ReadDiscography(JsonElement root)
    {
        var list = new List<ReleaseGroupDto>();
        if (!root.TryGetProperty("release-groups", out var groups) || groups.ValueKind != JsonValueKind.Array)
            return list;
        // Display-only de-duplication by title (ordinal, ignoring case); not a search or a DB uniqueness rule.
        var seenTitles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var g in groups.EnumerateArray())
        {
            var gid = Str(g, "id");
            var title = Str(g, "title");
            if (gid is null || title is null)
                continue;
            // Albums only: primary type Album and no secondary type (no live, compilation, remix, ...).
            if (Str(g, "primary-type") != "Album"
                || (g.TryGetProperty("secondary-types", out var secondary)
                    && secondary.ValueKind == JsonValueKind.Array && secondary.GetArrayLength() > 0))
                continue;
            var date = Str(g, "first-release-date");
            var year = date is { Length: >= 4 } ? date[..4] : null;
            list.Add(new ReleaseGroupDto(gid, title, year));
        }

        // Oldest first, unknown year last; the sort is stable, so equal years keep the provider order.
        var ordered = list.OrderBy(r => r.FirstReleaseYear is null).ThenBy(r => r.FirstReleaseYear, StringComparer.Ordinal);
        return ordered.Where(r => seenTitles.Add(r.Title)).ToList();
    }

    private static string? ReadWikidataId(JsonElement root)
    {
        if (!root.TryGetProperty("relations", out var rels) || rels.ValueKind != JsonValueKind.Array)
            return null;
        foreach (var rel in rels.EnumerateArray())
        {
            if (Str(rel, "type") != "wikidata" || !rel.TryGetProperty("url", out var url))
                continue;
            var resource = Str(url, "resource");
            var last = resource?[(resource.LastIndexOf('/') + 1)..];
            if (last is not null && QidRegex().IsMatch(last))
                return last;
        }
        return null;
    }

    private static string? ReadDescription(JsonElement entity)
    {
        if (!entity.TryGetProperty("descriptions", out var descriptions))
            return null;
        foreach (var lang in DescriptionLanguages)
            if (descriptions.TryGetProperty(lang, out var d) && Str(d, "value") is { Length: > 0 } value)
                return value;
        return null;
    }

    private static string? ReadP18(JsonElement entity)
    {
        if (!entity.TryGetProperty("claims", out var claims) || !claims.TryGetProperty("P18", out var p18)
            || p18.ValueKind != JsonValueKind.Array || p18.GetArrayLength() == 0)
            return null;
        var snak = p18[0];
        return snak.TryGetProperty("mainsnak", out var main) && main.TryGetProperty("datavalue", out var dv)
            ? Str(dv, "value")
            : null;
    }

    private static string? MetaValue(JsonElement meta, string key) =>
        meta.ValueKind == JsonValueKind.Object && meta.TryGetProperty(key, out var e) ? Str(e, "value") : null;

    private static string? Str(JsonElement e, string name) =>
        e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String
            ? v.GetString()
            : null;

    private static string? StripHtml(string? html) =>
        html is null ? null : System.Net.WebUtility.HtmlDecode(HtmlTagRegex().Replace(html, string.Empty)).Trim();

    [GeneratedRegex("^Q[0-9]+$")]
    private static partial Regex QidRegex();

    [GeneratedRegex("<[^>]*>")]
    private static partial Regex HtmlTagRegex();
}
