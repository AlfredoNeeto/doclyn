using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Doclyn.Application.DocumentClassIndexers.Create;
using Doclyn.Application.DocumentClassIndexers.GetByDocumentClass;
using Doclyn.Application.DocumentClassIndexers.Update;
using Doclyn.Domain.Constants;
using Doclyn.Domain.Entities;
using Doclyn.Domain.Enums;
using Doclyn.Infrastructure.Database;
using Doclyn.IntegrationTests.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Doclyn.IntegrationTests.DocumentClassIndexers;

public sealed class DocumentClassIndexersControllerTests : IClassFixture<CustomWebApplicationFactory>, IDisposable
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public DocumentClassIndexersControllerTests(CustomWebApplicationFactory factory)
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
    public async Task GetAll_With_Operator_Returns_Active_Indexers()
    {
        var user = TestAuthHelper.CreateOperator();
        var documentClass = await SeedAdminClassAndIndexerAsync(user, isActive: true);
        Authenticate(user);

        var response = await _client.GetAsync($"/api/document-classes/{documentClass.Id}/indexers");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<IReadOnlyCollection<DocumentClassIndexerResponse>>();
        Assert.NotNull(result);
        var item = Assert.Single(result);
        Assert.Equal("numeroProcesso", item.Name);
        Assert.True(item.HasRegexPattern);
    }

    [Fact]
    public async Task Create_With_Operator_Returns_403()
    {
        var user = TestAuthHelper.CreateOperator();
        var documentClass = await SeedDocumentClassAsync();
        await SeedUserAsync(user);
        Authenticate(user);

        var response = await _client.PostAsJsonAsync(
            $"/api/document-classes/{documentClass.Id}/indexers",
            new CreateDocumentClassIndexerCommand(
                Guid.Empty,
                "numeroContrato",
                "Numero do Contrato",
                "Numero do contrato.",
                IndexerDataType.Text,
                false,
                false,
                null,
                @"Contrato\s+n[º°]?\s*([\d\/.-]+)"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Create_With_Admin_Creates_Indexer()
    {
        var admin = TestAuthHelper.CreateAdmin();
        var documentClass = await SeedDocumentClassAsync();
        await SeedUserAsync(admin);
        Authenticate(admin);

        var response = await _client.PostAsJsonAsync(
            $"/api/document-classes/{documentClass.Id}/indexers",
            new CreateDocumentClassIndexerCommand(
                Guid.Empty,
                "numeroContrato",
                "Numero do Contrato",
                "Numero do contrato.",
                IndexerDataType.Text,
                false,
                false,
                null,
                @"Contrato\s+n[º°]?\s*([\d\/.-]+)"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DoclynDbContext>();
        Assert.True(await context.DocumentClassIndexers.AnyAsync(i => i.DocumentClassId == documentClass.Id && i.Name == "numeroContrato"));
    }

    [Fact]
    public async Task Update_With_Admin_Updates_Indexer()
    {
        var admin = TestAuthHelper.CreateAdmin();
        var documentClass = await SeedDocumentClassAsync();
        var indexer = await SeedIndexerAsync(documentClass.Id);
        await SeedUserAsync(admin);
        Authenticate(admin);

        var response = await _client.PutAsJsonAsync(
            $"/api/document-classes/{documentClass.Id}/indexers/{indexer.Id}",
            new UpdateDocumentClassIndexerCommand(
                Guid.Empty,
                Guid.Empty,
                "numeroProcesso",
                "Numero do Processo Atualizado",
                "Descricao atualizada.",
                IndexerDataType.Text,
                true,
                false,
                null,
                @"PROCESSO\s+ADMINISTRATIVO\s+N[º°]?\s*([\d\/.-]+)"));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DoclynDbContext>();
        var persisted = await context.DocumentClassIndexers.SingleAsync(i => i.Id == indexer.Id);
        Assert.Equal("Numero do Processo Atualizado", persisted.DisplayName);
        Assert.Equal("Descricao atualizada.", persisted.Description);
    }

    [Fact]
    public async Task Delete_With_Admin_Disables_Indexer()
    {
        var admin = TestAuthHelper.CreateAdmin();
        var documentClass = await SeedDocumentClassAsync();
        var indexer = await SeedIndexerAsync(documentClass.Id);
        await SeedUserAsync(admin);
        Authenticate(admin);

        var response = await _client.DeleteAsync($"/api/document-classes/{documentClass.Id}/indexers/{indexer.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DoclynDbContext>();
        var persisted = await context.DocumentClassIndexers.SingleAsync(i => i.Id == indexer.Id);
        Assert.False(persisted.IsActive);
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

    private async Task<DocumentClass> SeedDocumentClassAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DoclynDbContext>();
        var documentClass = DocumentClass.Create(
            DocumentTypes.RelatorioTecnicoPreliminar,
            "ADMINISTRATIVO",
            "PROCESSO_ADMINISTRATIVO",
            "Classe de relatorio tecnico preliminar.",
            isSystemDefined: true);
        context.DocumentClasses.Add(documentClass);
        await context.SaveChangesAsync();
        return documentClass;
    }

    private async Task<DocumentClassIndexer> SeedIndexerAsync(Guid documentClassId, bool isActive = true)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DoclynDbContext>();
        var indexer = DocumentClassIndexer.Create(
            documentClassId,
            "numeroProcesso",
            "Numero do Processo",
            "Numero do processo administrativo.",
            IndexerDataType.Text,
            isRequired: true,
            isMultiple: false,
            regexPattern: @"PROCESSO\s+ADMINISTRATIVO\s+N[º°]?\s*([\d\/.-]+)");

        if (!isActive)
        {
            indexer.Disable();
        }

        context.DocumentClassIndexers.Add(indexer);
        await context.SaveChangesAsync();
        return indexer;
    }

    private async Task<DocumentClass> SeedAdminClassAndIndexerAsync(User user, bool isActive)
    {
        await SeedUserAsync(user);
        var documentClass = await SeedDocumentClassAsync();
        await SeedIndexerAsync(documentClass.Id, isActive);
        return documentClass;
    }

    private void Authenticate(User user)
    {
        var token = TestAuthHelper.GenerateToken(user);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }
}
