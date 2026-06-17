namespace Doclyn.Application.Documents.GetInsights;

public sealed record DocumentInsightResponse(
    Guid Id,
    string Type,
    string Severity,
    string Title,
    string Message,
    decimal Confidence,
    string Source,
    string? RelatedFieldName,
    DateTime CreatedAt);
