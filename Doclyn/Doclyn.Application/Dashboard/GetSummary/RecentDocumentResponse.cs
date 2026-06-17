namespace Doclyn.Application.Dashboard.GetSummary;

public sealed record RecentDocumentResponse(
    Guid Id,
    string FileName,
    string DocumentStatus,
    string? DocumentClass,
    decimal? AverageConfidence,
    int InsightsCount,
    int NeedsReviewCount,
    DateTime CreatedAt);
