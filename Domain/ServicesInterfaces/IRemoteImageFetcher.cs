namespace MyVocaList.Domain.ServicesInterfaces;

/// <summary>Downloads one remote picture for the venue form (REQ-VEN-38, 39). The caller disposes the returned stream.</summary>
public interface IRemoteImageFetcher
{
    /// <summary>
    /// The downloaded bytes as a seekable stream, or null when the address is not https, the answer is not an image,
    /// is too large, is slower than the bound, or the request fails. Expected failures never throw.
    /// </summary>
    Task<Stream?> FetchAsync(Uri address, CancellationToken ct = default);
}
