using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Doclyn.Application.Dashboard.GetSummary;
using Doclyn.Domain.Entities;
using Doclyn.Infrastructure.Database;
using Doclyn.IntegrationTests.Common;
using Microsoft.Extensions.DependencyInjection;

namespace Doclyn.IntegrationTests.Dashboard;

public sealed class DashboardControllerTests : IClassFixture<CustomWebApplicationFactory>, IDisposable
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public DashboardControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        CleanDatabaseAsync().GetAwaiter().GetResult();
    }

    public void Dispose()
    {
        _client.Dispose();
    }

    [Fact]
    public async Task GetSummary_Without_Jwt_Returns_401()
    {
        var response = await _client.GetAsync("/api/dashboard/summary");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetSummary_With_Jwt_Returns_200_And_All_Top_Level_Blocks_Are_Non_Null()
    {
        var user = TestAuthHelper.CreateOperator("dashboard@doclyn.local");
        await SeedUserAsync(user);
        Authenticate(user);

        var response = await _client.GetAsync("/api/dashboard/summary");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<DashboardSummaryResponse>();
        Assert.NotNull(payload);
        Assert.NotNull(payload.Documents);
        Assert.NotNull(payload.Quality);
        Assert.NotNull(payload.Insights);
        Assert.NotNull(payload.Classes);
        Assert.NotNull(payload.RecentDocuments);
        Assert.NotNull(payload.AttentionRequired);
    }

    private async Task CleanDatabaseAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DoclynDbContext>();
        context.DocumentClassExamples.RemoveRange(context.DocumentClassExamples);
        context.DocumentClassIndexers.RemoveRange(context.DocumentClassIndexers);
        context.DocumentClasses.RemoveRange(context.DocumentClasses);
        context.ProcessingLogs.RemoveRange(context.ProcessingLogs);
        context.ExtractedData.RemoveRange(context.ExtractedData);
        context.Documents.RemoveRange(context.Documents);
        context.Users.RemoveRange(context.Users);
        await context.SaveChangesAsync();
    }

    private async Task SeedUserAsync(User user)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DoclynDbContext>();
        context.Users.Add(user);
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task Operator_Does_Not_See_Other_User_Documents_In_Summary()
    {
        var owner = TestAuthHelper.CreateOperator("dash-owner@doclyn.local");
        var other = TestAuthHelper.CreateOperator("dash-other@doclyn.local");

        await SeedUserAsync(owner);
        await SeedUserAsync(other);

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<DoclynDbContext>();
            context.Documents.Add(Document.Create(owner.Id, "own.pdf", "h1", "p1", Doclyn.Domain.Constants.DocumentTypes.Unknown));
            context.Documents.Add(Document.Create(other.Id, "other.pdf", "h2", "p2", Doclyn.Domain.Constants.DocumentTypes.Unknown));
            await context.SaveChangesAsync();
        }

        Authenticate(owner);
        var response = await _client.GetAsync("/api/dashboard/summary");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<DashboardSummaryResponse>();
        Assert.NotNull(payload);
        Assert.Equal(1, payload.Documents.Total);
    }

    [Fact]
    public async Task Admin_Sees_Global_Summary()
    {
        var admin = TestAuthHelper.CreateAdmin("dash-admin@doclyn.local");
        await SeedUserAsync(admin);

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<DoclynDbContext>();
            context.Documents.Add(Document.Create(admin.Id, "a.pdf", "h1", "p1", Doclyn.Domain.Constants.DocumentTypes.Unknown));
            context.Documents.Add(Document.Create(admin.Id, "b.pdf", "h2", "p2", Doclyn.Domain.Constants.DocumentTypes.Unknown));
            context.Documents.Add(Document.Create(admin.Id, "c.pdf", "h3", "p3", Doclyn.Domain.Constants.DocumentTypes.Unknown));
            await context.SaveChangesAsync();
        }

        Authenticate(admin);
        var response = await _client.GetAsync("/api/dashboard/summary");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<DashboardSummaryResponse>();
        Assert.NotNull(payload);
        Assert.Equal(3, payload.Documents.Total);
    }

    private void Authenticate(User user)
    {
        var token = TestAuthHelper.GenerateToken(user);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }
}
