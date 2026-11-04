namespace MyVocaList.Services;

/// <summary>
/// Thrown by an <see cref="IMusicMetadataProvider"/> when the remote provider answered with an error
/// payload (for example a Deezer error body returned with HTTP 200). Callers map it to an unavailable state.
/// </summary>
public sealed class MusicProviderException(string message, Exception? innerException = null)
    : Exception(message, innerException);
