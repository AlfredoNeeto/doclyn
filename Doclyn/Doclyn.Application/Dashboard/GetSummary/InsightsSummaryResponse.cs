namespace Doclyn.Application.Dashboard.GetSummary;

public sealed record InsightsSummaryResponse(
    int Total,
    int Critical,
    int Warning,
    int Info,
    int Success);
