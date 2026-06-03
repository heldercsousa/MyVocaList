using MyVocaList.Contracts.DTOs;

namespace MyVocaList.Domain.ServicesInterfaces;

public interface IFeedbackService
{
    /// <summary>Submits a user suggestion as a GitHub Issue.</summary>
    /// <returns>(true, null) on success; (false, errorMessage) on failure.</returns>
    Task<(bool success, string? error)> SubmitAsync(FeedbackSubmission submission, CancellationToken ct = default);
}
