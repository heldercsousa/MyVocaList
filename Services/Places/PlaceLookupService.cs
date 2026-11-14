using System.Globalization;
using MyVocaList.Contracts.DTOs.Venues;
using MyVocaList.Domain.Constants;
using MyVocaList.Domain.ServicesInterfaces;
using MyVocaList.Domain.Rules;

namespace MyVocaList.Services.Places;

/// <inheritdoc />
/// <remarks>
/// Every lookup goes through <see cref="PlaceLookupGate"/>, which owns the usage rules (REQ-VEN-34). A failure
/// (provider exception or the 5,000 ms bound) is thrown inside the gate, so it is never remembered, and is turned
/// into <c>Unavailable</c> here (REQ-VEN-28).
/// </remarks>
public sealed class PlaceLookupService : IPlaceLookupService
{
    // REQ-VEN-25: search starts at three characters; five results per search (design § 2.4).
    private const int MinimumSearchLength = 3;
    private const int SearchResultLimit = 5;
    private const string FallbackLanguage = "en";

    private readonly IGeocodingProvider _provider;
    private readonly PlaceLookupGate _gate;
    private readonly INetworkStatus _networkStatus;
    private readonly ILogger<PlaceLookupService> _logger;

    public PlaceLookupService(
        IGeocodingProvider provider,
        PlaceLookupGate gate,
        INetworkStatus networkStatus,
        ILogger<PlaceLookupService> logger)
    {
        _provider = provider;
        _gate = gate;
        _networkStatus = networkStatus;
        _logger = logger;
    }

    /// <inheritdoc />
    // [AC] REQ-VEN-26/27/28/34: offline skip, mapped answer with tapped coordinates, bound and failure mapping
    public async Task<PlaceLookupResult> ReverseAsync(double latitude, double longitude, CancellationToken ct = default)
    {
        if (!_networkStatus.HasInternetAccess)
            return new PlaceLookupResult(PlaceLookupStatus.Unavailable, null);

        var language = ResolveLanguage();
        var key = FormattableString.Invariant($"r|{latitude:F5}|{longitude:F5}|{language}");
        try
        {
            return await _gate.RunAsync(
                key,
                token => ReverseCoreAsync(latitude, longitude, language, token),
                ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Reverse place lookup failed at {Latitude}, {Longitude}", latitude, longitude);
            return new PlaceLookupResult(PlaceLookupStatus.Unavailable, null);
        }
    }

    /// <inheritdoc />
    // [AC] REQ-VEN-25/28/34: three-character guard, offline skip, five results, empty list is Found
    public async Task<PlaceSearchResult> SearchAsync(string text, CancellationToken ct = default)
    {
        var trimmed = text.Trim();
        if (trimmed.Length < MinimumSearchLength)
            return new PlaceSearchResult(PlaceLookupStatus.Found, []);

        if (!_networkStatus.HasInternetAccess)
            return new PlaceSearchResult(PlaceLookupStatus.Unavailable, []);

        var language = ResolveLanguage();
        var key = FormattableString.Invariant($"s|{trimmed}|{language}");
        try
        {
            return await _gate.RunAsync(
                key,
                token => SearchCoreAsync(trimmed, language, token),
                ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Place search failed for query '{Query}'", trimmed);
            return new PlaceSearchResult(PlaceLookupStatus.Unavailable, []);
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    // The bound starts when the request is sent, not while it waits for its turn (REQ-VEN-28).
    private async Task<PlaceLookupResult> ReverseCoreAsync(double latitude, double longitude, string language, CancellationToken token)
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(token);
        linked.CancelAfter(SearchConstants.PlaceLookupTimeoutMilliseconds);

        var record = await _provider.ReverseAsync(latitude, longitude, language, linked.Token);
        // A provider that returns after the bound fired must not have its answer remembered.
        linked.Token.ThrowIfCancellationRequested();

        if (record is null)
            return NoAddressAt(latitude, longitude);

        var mapped = NominatimAddressMapper.ToPlace(record);
        if (VenueAddressRules.IsEmpty(mapped.Address))
            return NoAddressAt(latitude, longitude);

        // The stored point is the one the user tapped, not the provider's rounded coordinates (REQ-VEN-26).
        return new PlaceLookupResult(PlaceLookupStatus.Found, mapped with { Latitude = latitude, Longitude = longitude });
    }

    private async Task<PlaceSearchResult> SearchCoreAsync(string trimmed, string language, CancellationToken token)
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(token);
        linked.CancelAfter(SearchConstants.PlaceLookupTimeoutMilliseconds);

        var records = await _provider.SearchAsync(trimmed, language, SearchResultLimit, linked.Token);
        linked.Token.ThrowIfCancellationRequested();

        var places = records.Select(NominatimAddressMapper.ToPlace).ToList();
        return new PlaceSearchResult(PlaceLookupStatus.Found, places);
    }

    private static PlaceLookupResult NoAddressAt(double latitude, double longitude) =>
        new(PlaceLookupStatus.NoAddress, new VenuePlace(latitude, longitude, VenueAddress.Empty, null, string.Empty));

    private static string ResolveLanguage()
    {
        var name = CultureInfo.CurrentUICulture.Name;
        return string.IsNullOrEmpty(name) ? FallbackLanguage : name;
    }
}
