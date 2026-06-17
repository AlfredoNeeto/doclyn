namespace Doclyn.Application.Documents.GenerateInsights;

public sealed record GenerateDocumentInsightsResponse(
    Guid DocumentId,
    int GeneratedCount);
