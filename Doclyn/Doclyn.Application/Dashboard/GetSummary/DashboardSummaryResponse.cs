namespace Doclyn.Application.Dashboard.GetSummary;

public sealed record DashboardSummaryResponse(
    DocumentsSummaryResponse Documents,
    QualitySummaryResponse Quality,
    InsightsSummaryResponse Insights,
    ClassesSummaryResponse Classes,
    IReadOnlyList<RecentDocumentResponse> RecentDocuments,
    IReadOnlyList<AttentionRequiredResponse> AttentionRequired);
