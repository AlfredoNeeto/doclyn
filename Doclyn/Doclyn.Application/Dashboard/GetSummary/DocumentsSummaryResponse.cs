namespace Doclyn.Application.Dashboard.GetSummary;

public sealed record DocumentsSummaryResponse(
    int Total,
    int Pending,
    int Processing,
    int Processed,
    int Failed);
