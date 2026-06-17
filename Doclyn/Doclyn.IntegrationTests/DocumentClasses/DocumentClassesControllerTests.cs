using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Doclyn.Application.DocumentClasses.GetAll;
using Doclyn.Application.DocumentClasses.GetById;
using Doclyn.Application.DocumentClasses.GetExamples;
using Doclyn.Application.Documents.Processing;
using Doclyn.Application.Documents.Process;
using Doclyn.Domain.Constants;
using Doclyn.Domain.Entities;
using Doclyn.Domain.Enums;
using Doclyn.Infrastructure.Database;
using Doclyn.IntegrationTests.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit.Abstractions;

namespace Doclyn.IntegrationTests.DocumentClasses;

public sealed class DocumentClassesControllerTests : IClassFixture<CustomWebApplicationFactory>, IDisposable
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly ITestOutputHelper _testOutputHelper;

    public DocumentClassesControllerTests(CustomWebApplicationFactory factory, ITestOutputHelper testOutputHelper)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _testOutputHelper = testOutputHelper;
        _factory.FileStorageService.DownloadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_ => new MemoryStream([1, 2, 3]));
        CleanDatabaseAsync().GetAwaiter().GetResult();
    }

    public void Dispose()
    {
        _client.Dispose();
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

    [Fact]
    public async Task GetAll_Without_Jwt_Returns_401()
    {
        var response = await _client.GetAsync("/api/document-classes");
        var content = await response.Content.ReadAsStringAsync();
        _testOutputHelper.WriteLine($"Status: {response.StatusCode}, Body: {content}");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_With_Valid_Jwt_Returns_Seeded_Classes()
    {
        var user = TestAuthHelper.CreateOperator();
        await SeedUserAsync(user);
        await SeedDocumentClassesAsync();
        Authenticate(user);

        var response = await _client.GetAsync("/api/document-classes");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<GetDocumentClassesResponse>();
        Assert.NotNull(result);
        Assert.Contains(result.Items, item => item.Name == "RELATORIO_TECNICO_PRELIMINAR");
        Assert.Contains(result.Items, item => item.Name == "CONTRATO_ADMINISTRATIVO");
        Assert.Contains(result.Items, item => item.Name == "OFICIO");
        Assert.Contains(result.Items, item => item.Name == "NOTA_FISCAL");
        Assert.Contains(result.Items, item => item.Name == "PETICAO_JUDICIAL");
        Assert.Contains(result.Items, item => item.Name == "DOCUMENTO_DESCONHECIDO");
    }

    [Fact]
    public async Task GetById_Returns_Class_Details()
    {
        var user = TestAuthHelper.CreateOperator();
        await SeedUserAsync(user);
        await SeedDocumentClassesAsync();
        Authenticate(user);

        var listResponse = await _client.GetAsync("/api/document-classes");
        var listResult = await listResponse.Content.ReadFromJsonAsync<GetDocumentClassesResponse>();
        var documentClass = listResult!.Items.First();

        var response = await _client.GetAsync($"/api/document-classes/{documentClass.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<GetDocumentClassByIdResponse>();
        Assert.NotNull(result);
        Assert.Equal(documentClass.Id, result.Id);
        Assert.Equal(documentClass.Name, result.Name);
    }

    [Fact]
    public async Task Process_Creates_Document_Class_And_Registers_Example()
    {
        var user = TestAuthHelper.CreateOperator();
        var document = Document.Create(user.Id, "file.pdf", "hash", "documents/test/original.pdf", DocumentTypes.Unknown);

        _factory.PdfTextExtractor.ExtractTextAsync(Arg.Any<Stream>(), Arg.Any<CancellationToken>())
            .Returns("""
                RELATÓRIO TÉCNICO PRELIMINAR
                PROCESSO ADMINISTRATIVO Nº 2026/98765
                CONTRATO nº 45/2026
                PREFEITURA MUNICIPAL DE VALE VERDE
                CNPJ 12.345.678/0001-99
                """);
        _factory.AiDocumentClassifier.ClassifyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new DocumentClassificationResult(DocumentTypes.RelatorioTecnicoPreliminar, "ADMINISTRATIVO", "PROCESSO_ADMINISTRATIVO", 0.98));
        _factory.AiStructuredDataExtractor.ExtractAsync(
                Arg.Any<string>(),
                Arg.Any<DocumentClass>(),
                Arg.Any<IReadOnlyCollection<DocumentClassIndexer>>(),
                Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, object?> { ["numeroProcesso"] = "2026/98765" });

        await SeedUserAndDocumentAsync(user, document);
        await SeedDocumentClassesAsync();
        await SeedRelatorioIndexersAsync();
        Authenticate(user);

        var response = await _client.PostAsync($"/api/documents/{document.Id}/process", content: null);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DoclynDbContext>();

        var documentClass = await context.DocumentClasses
            .FirstOrDefaultAsync(dc => dc.Name == DocumentTypes.RelatorioTecnicoPreliminar);
        Assert.NotNull(documentClass);

        var example = await context.DocumentClassExamples
            .FirstOrDefaultAsync(dce => dce.DocumentClassId == documentClass.Id && dce.DocumentId == document.Id);
        Assert.NotNull(example);
        Assert.Equal(0.98m, example.Confidence);

        var extractedData = await context.ExtractedData.SingleAsync(ed => ed.DocumentId == document.Id);
        Assert.Equal(documentClass.Id, extractedData.Data.RootElement.GetProperty("classification").GetProperty("documentClassId").GetGuid());
        Assert.Equal(DocumentTypes.RelatorioTecnicoPreliminar, extractedData.Data.RootElement.GetProperty("classification").GetProperty("documentType").GetString());
        Assert.Equal("2026/98765", extractedData.Data.RootElement.GetProperty("indexers").GetProperty("numeroProcesso").GetProperty("value").GetString());
    }

    [Fact]
    public async Task GetExamples_Returns_Registered_Examples()
    {
        var user = TestAuthHelper.CreateOperator();
        var document = Document.Create(user.Id, "file.pdf", "hash", "documents/test/original.pdf", DocumentTypes.Unknown);
        var documentClass = DocumentClass.Create(
            DocumentTypes.RelatorioTecnicoPreliminar,
            "ADMINISTRATIVO",
            "PROCESSO_ADMINISTRATIVO",
            "Descrição.",
            isSystemDefined: true);
        var example = DocumentClassExample.Create(documentClass.Id, document.Id, 0.95m);

        await SeedUserDocumentClassAndExampleAsync(user, document, documentClass, example);
        Authenticate(user);

        var response = await _client.GetAsync($"/api/document-classes/{documentClass.Id}/examples");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<List<DocumentClassExampleResponse>>();
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal(document.Id, result[0].DocumentId);
        Assert.Equal("file.pdf", result[0].FileName);
        Assert.Equal(0.95m, result[0].Confidence);
    }

    [Fact]
    public async Task GetTop_Returns_Top_Classes_Ordered_By_Example_Count()
    {
        var user = TestAuthHelper.CreateOperator();
        var dc1 = DocumentClass.Create("RELATORIO_TECNICO_PRELIMINAR", "ADMINISTRATIVO", "PROCESSO_ADMINISTRATIVO", isSystemDefined: true);
        var dc2 = DocumentClass.Create("CONTRATO_ADMINISTRATIVO", "ADMINISTRATIVO", "CONTRATOS", isSystemDefined: true);

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<DoclynDbContext>();
            context.Users.Add(user);
            context.DocumentClasses.Add(dc1);
            context.DocumentClasses.Add(dc2);
            await context.SaveChangesAsync();
        }

        var document = Document.Create(user.Id, "file.pdf", "hash", "documents/test/original.pdf", DocumentTypes.Unknown);
        var example1 = DocumentClassExample.Create(dc1.Id, document.Id, 0.98m);
        var example2 = DocumentClassExample.Create(dc1.Id, document.Id, 0.97m);

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<DoclynDbContext>();
            context.Documents.Add(document);
            context.DocumentClassExamples.Add(example1);
            context.DocumentClassExamples.Add(example2);
            await context.SaveChangesAsync();
        }

        Authenticate(user);

        var response = await _client.GetAsync("/api/document-classes/top?take=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private async Task SeedUserAsync(User user)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DoclynDbContext>();
        context.Users.Add(user);
        await context.SaveChangesAsync();
    }

    private async Task SeedDocumentClassesAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DoclynDbContext>();

        var systemClasses = new[]
        {
            ("RELATORIO_TECNICO_PRELIMINAR", "ADMINISTRATIVO", "PROCESSO_ADMINISTRATIVO"),
            ("CONTRATO_ADMINISTRATIVO", "ADMINISTRATIVO", "PROCESSO_ADMINISTRATIVO"),
            ("OFICIO", "ADMINISTRATIVO", "COMUNICACAO"),
            ("NOTA_FISCAL", "FISCAL", "TRIBUTARIO"),
            ("PETICAO_JUDICIAL", "JURIDICO", "PROCESSO_JUDICIAL"),
            ("DOCUMENTO_DESCONHECIDO", "OUTROS", "NAO_CLASSIFICADO")
        };

        foreach (var (name, group, subGroup) in systemClasses)
        {
            if (!await context.DocumentClasses.AnyAsync(dc => dc.Name == name))
            {
                context.DocumentClasses.Add(DocumentClass.Create(name, group, subGroup, $"Classe {name}.", isSystemDefined: true));
            }
        }

        await context.SaveChangesAsync();
    }

    private async Task SeedUserAndDocumentAsync(User user, Document document)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DoclynDbContext>();
        context.Users.Add(user);
        context.Documents.Add(document);
        await context.SaveChangesAsync();
    }

    private async Task SeedRelatorioIndexersAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DoclynDbContext>();
        var documentClass = await context.DocumentClasses.SingleAsync(dc => dc.Name == DocumentTypes.RelatorioTecnicoPreliminar);

        if (!await context.DocumentClassIndexers.AnyAsync(i => i.DocumentClassId == documentClass.Id && i.Name == "numeroProcesso"))
        {
            context.DocumentClassIndexers.Add(DocumentClassIndexer.Create(
                documentClass.Id,
                "numeroProcesso",
                "Numero do Processo",
                "Numero do processo administrativo.",
                IndexerDataType.Text,
                isRequired: true,
                isMultiple: false,
                regexPattern: @"PROCESSO\s+ADMINISTRATIVO\s+N[º°]?\s*([\d\/.-]+)"));
        }

        await context.SaveChangesAsync();
    }

    private async Task SeedUserDocumentClassAndExampleAsync(
        User user,
        Document document,
        DocumentClass documentClass,
        DocumentClassExample example)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DoclynDbContext>();
        context.Users.Add(user);
        context.Documents.Add(document);
        context.DocumentClasses.Add(documentClass);
        context.DocumentClassExamples.Add(example);
        await context.SaveChangesAsync();
    }

    private void Authenticate(User user)
    {
        var token = TestAuthHelper.GenerateToken(user);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }
}
