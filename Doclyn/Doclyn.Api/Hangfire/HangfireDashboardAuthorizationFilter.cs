using Hangfire.Dashboard;

namespace Doclyn.Api.Hangfire;

public sealed class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        var user = httpContext.User;

        return user.Identity?.IsAuthenticated == true && user.IsInRole("Admin");
    }
}
