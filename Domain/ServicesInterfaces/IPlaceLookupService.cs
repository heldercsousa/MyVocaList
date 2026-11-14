using MyVocaList.Contracts.DTOs.Venues;

namespace MyVocaList.Domain.ServicesInterfaces;

/// <summary>
/// Looks up places for the venue map picker (REQ-VEN-24..28). Neither method throws, except
/// <see cref="OperationCanceledException"/> when the caller's own token is cancelled: offline use,
/// provider failure and the 5,000 ms bound all return <c>Unavailable</c>.
/// </summary>
public interface IPlaceLookupService
{
    /// <summary>Returns the address at a point. Coordinates in the result are the requested ones.</summary>
    /// <param name="latitude">Latitude of the tapped point, decimal degrees.</param>
    /// <param name="longitude">Longitude of the tapped point, decimal degrees.</param>
    /// <param name="ct">Caller's cancellation token.</param>
    Task<PlaceLookupResult> ReverseAsync(double latitude, double longitude, CancellationToken ct = default);

    /// <summary>Returns up to five places matching a text. Text shorter than three characters is not searched.</summary>
    /// <param name="text">The text typed by the user; trimmed before use.</param>
    /// <param name="ct">Caller's cancellation token.</param>
    Task<PlaceSearchResult> SearchAsync(string text, CancellationToken ct = default);
}
