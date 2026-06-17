namespace Doclyn.Application.Dashboard.GetSummary;

public sealed record ClassesSummaryResponse(
    int Total,
    IReadOnlyList<DashboardClassUsageResponse> MostUsed);
