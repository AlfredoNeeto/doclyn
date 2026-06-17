namespace Doclyn.Application.Dashboard.GetSummary;

public sealed record AttentionRequiredResponse(
    Guid DocumentId,
    string FileName,
    string Reason,
    string Severity,
    DateTime CreatedAt);
