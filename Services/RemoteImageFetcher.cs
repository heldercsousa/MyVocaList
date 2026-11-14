using MyVocaList.Domain.Constants;
using MyVocaList.Domain.ServicesInterfaces;

namespace MyVocaList.Services;

/// <summary>
/// Downloads one remote picture for the venue form (REQ-VEN-38, 39). https only; the answer must be an image no larger
/// than the image store accepts; the wait is bounded by <see cref="SearchConstants.PlaceLookupTimeoutMilliseconds"/>.
/// Expected failures return null and are logged; caller cancellation propagates.
/// </summary>
public sealed class RemoteImageFetcher(HttpClient httpClient, ILogger<RemoteImageFetcher> logger) : IRemoteImageFetcher
{
    /// <inheritdoc />
    public async Task<Stream?> FetchAsync(Uri address, CancellationToken ct = default)
    {
        if (!address.IsAbsoluteUri || address.Scheme != Uri.UriSchemeHttps)
        {
            logger.LogWarning("Venue picture skipped: the address is not https");
            return null;
        }

        using var bound = CancellationTokenSource.CreateLinkedTokenSource(ct);
        bound.CancelAfter(SearchConstants.PlaceLookupTimeoutMilliseconds);
        try
        {
            using var response = await httpClient.GetAsync(address, bound.Token);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Venue picture download answered {StatusCode}", (int)response.StatusCode);
                return null;
            }

            var mediaType = response.Content.Headers.ContentType?.MediaType;
            if (mediaType is null || !mediaType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                logger.LogWarning("Venue picture download answered a non-image content type");
                return null;
            }

            if (response.Content.Headers.ContentLength > ImageStore.MaxSourceBytes)
            {
                logger.LogWarning("Venue picture download is larger than the accepted size");
                return null;
            }

            var bytes = await response.Content.ReadAsByteArrayAsync(bound.Token);
            if (bytes.Length > ImageStore.MaxSourceBytes)
            {
                logger.LogWarning("Venue picture download is larger than the accepted size");
                return null;
            }

            return new MemoryStream(bytes, writable: false);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ex is OperationCanceledException or HttpRequestException)
        {
            logger.LogWarning("Venue picture download failed: {Reason}", ex.GetType().Name);
            return null;
        }
    }
}
