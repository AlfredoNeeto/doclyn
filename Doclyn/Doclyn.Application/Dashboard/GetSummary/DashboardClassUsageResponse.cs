namespace Doclyn.Application.Dashboard.GetSummary;

public sealed record DashboardClassUsageResponse(
    Guid Id,
    string Name,
    string DisplayName,
    int DocumentsCount);
