using Doclyn.Domain.Enums;

namespace Doclyn.Application.Documents.Insights;

public sealed record DocumentInsightResult(
    DocumentInsightType Type,
    DocumentInsightSeverity Severity,
    string Title,
    string Message,
    decimal Confidence,
    DocumentInsightSource Source,
    string? RelatedFieldName = null);
