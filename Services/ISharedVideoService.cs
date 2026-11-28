using MyVocaList.Contracts.DTOs;

namespace MyVocaList.Services;

/// <summary>REQ-MSRC-11, -12: routing and prefill of a YouTube link shared into the app.</summary>
public interface ISharedVideoService
{
    /// <summary>
    /// The video id in the shared text and the stored song that holds it (REQ-MSRC-11), or null when the text has no
    /// YouTube link. Never throws except <see cref="OperationCanceledException"/>.
    /// </summary>
    Task<SharedVideoTarget?> ResolveTargetAsync(string sharedText, CancellationToken ct = default);

    /// <summary>
    /// REQ-MSRC-12: the cleaned oEmbed title of the video and the best MusicBrainz recording for it, or null when the
    /// title cannot be read (offline, unknown video). Never throws except <see cref="OperationCanceledException"/>.
    /// </summary>
    Task<VideoPrefill?> PrefillAsync(string videoId, CancellationToken ct = default);
}
