namespace MyVocaList.Services.Places;

/// <summary>
/// Looks up places and addresses at a coordinate or by text (REQ-VEN-25, 26, 34).
/// Both methods throw on transport or protocol failure and let <see cref="OperationCanceledException"/> propagate.
/// </summary>
public interface IGeocodingProvider
{
    /// <summary>Returns the record at the coordinate, or null when the service has nothing there.</summary>
    Task<GeocodingRecord?> ReverseAsync(double latitude, double longitude, string language, CancellationToken ct = default);

    /// <summary>Returns the records matching the text, in the service's order; empty when nothing matches.</summary>
    Task<IReadOnlyList<GeocodingRecord>> SearchAsync(string text, string language, int limit, CancellationToken ct = default);
}
