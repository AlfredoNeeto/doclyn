using MediatR;

namespace Doclyn.Application.Dashboard.GetSummary;

public sealed record GetDashboardSummaryQuery() : IRequest<DashboardSummaryResponse>;
