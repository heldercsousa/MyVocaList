namespace MyVocaList.Contracts.DTOs;

public enum FeedbackCategory { BugReport, FeatureRequest, Other }

public record FeedbackSubmission(
    FeedbackCategory Category,
    string Message,
    string? Email);
