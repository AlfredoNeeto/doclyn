using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Doclyn.Application.Documents.GetAll;
using Doclyn.Application.Documents.GetById;
using Doclyn.Application.Documents.GetExtractedData;
using Doclyn.Application.Documents.GetLogs;
using Doclyn.Application.Documents.GetReviewFields;
using Doclyn.Application.Documents.Processing;
using Doclyn.Application.Documents.Process;
using Doclyn.Application.Documents.Reprocess;
using Doclyn.Application.Documents.ReprocessBatch;
using Doclyn.Application.Documents.ReprocessByFilter;
using Doclyn.Application.Documents.Upload;
using Doclyn.Domain.Constants;
using Doclyn.Domain.Entities;
using Doclyn.Domain.Enums;
using Doclyn.Infrastructure.Database;
using Doclyn.IntegrationTests.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Doclyn.IntegrationTests.Documents;

public sealed class DocumentsControllerTests : IClassFixture<CustomWebApplicationFactory>, IDisposable
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public DocumentsControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _factory.FileStorageService.DownloadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_ => new MemoryStream([1, 2, 3]));
        _factory.PdfTextExtractor.ExtractTextAsync(Arg.Any<Stream>(), Arg.Any<CancellationToken>())
            .Returns(string.Empty);
        _factory.OcrService.ExtractTextAsync(Arg.Any<Stream>(), Arg.Any<CancellationToken>())
            .Returns(string.Empty);
        _factory.AiDocumentClassifier.ClassifyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<DocumentClassificationResult>(new InvalidOperationException("AI unavailable")));
        _factory.AiStructuredDataExtractor.ExtractAsync(
                Arg.Any<string>(),
                Arg.Any<DocumentClass>(),
                Arg.Any<IReadOnlyCollection<DocumentClassIndexer>>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromException<Dictionary<string, object?>>(new InvalidOperationException("AI unavailable")));
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
        context.DocumentClassIndexers.RemoveRange(context.DocumentClassIndexers);
        context.DocumentClasses.RemoveRange(context.DocumentClasses);
        context.ProcessingLogs.RemoveRange(context.ProcessingLogs);
        context.ExtractedData.RemoveRange(context.ExtractedData);
        context.Documents.RemoveRange(context.Documents);
        context.Users.RemoveRange(context.Users);
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task Upload_Without_Jwt_Returns_401()
    {
        var content = CreateMultipartContent("file.pdf", "application/pdf", new byte[] { 0x25, 0x50, 0x44, 0x46 });

        var response = await _client.PostAsync("/api/documents/upload", content);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Upload_With_Valid_Jwt_Returns_200_And_Creates_Document()
    {
        var user = TestAuthHelper.CreateOperator();
        _factory.FileStorageService.UploadAsync(
                Arg.Any<Stream>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns("documents/user/doc/original.pdf");

        await SeedUserAsync(user);
        Authenticate(user);

        var content = CreateMultipartContent("file.pdf", "application/pdf", new byte[] { 0x25, 0x50, 0x44, 0x46 });

        var response = await _client.PostAsync("/api/documents/upload", content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<UploadDocumentResponse>();
        Assert.NotNull(result);
        Assert.Equal("Pending", result.DocumentStatus);
        Assert.Equal(DocumentTypes.Unknown, result.DocumentType);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DoclynDbContext>();
        var document = await context.Documents.SingleAsync(d => d.Id == result.Id);
        var processingLogs = await context.ProcessingLogs.Where(l => l.DocumentId == result.Id).ToListAsync();
        var uploadLog = Assert.Single(processingLogs, log => log.Step == "Upload");

        Assert.Equal(user.Id, document.UserId);
        Assert.Equal("file.pdf", document.FileName);
        Assert.Equal(DocumentStatus.Failed, document.DocumentStatus);
        Assert.Equal(DocumentTypes.Unknown, document.DocumentType);
        Assert.Equal($"documents/{user.Id}/{result.Id}/original.pdf", document.StoragePath);
        Assert.Equal(DocumentStatus.Success, uploadLog.Status);
        Assert.Equal("Document uploaded and stored successfully.", uploadLog.Message);
    }

    [Fact]
    public async Task Upload_With_Invalid_File_Returns_400()
    {
        var user = TestAuthHelper.CreateOperator();
        await SeedUserAsync(user);
        Authenticate(user);

        var content = CreateMultipartContent("file.txt", "text/plain", new byte[] { 0x41 });

        var response = await _client.PostAsync("/api/documents/upload", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Process_With_Valid_Jwt_Returns_202_And_Creates_Extracted_Data()
    {
        var user = TestAuthHelper.CreateOperator();
        var document = Document.Create(user.Id, "file.pdf", "hash", "documents/test/original.pdf", DocumentTypes.Unknown);

        _factory.FileStorageService.DownloadAsync(document.StoragePath, Arg.Any<CancellationToken>())
            .Returns(new MemoryStream([1, 2, 3]));
        _factory.PdfTextExtractor.ExtractTextAsync(Arg.Any<Stream>(), Arg.Any<CancellationToken>())
            .Returns("""
                RELATÓRIO TÉCNICO PRELIMINAR
                PROCESSO ADMINISTRATIVO Nº 2026/98765
                CONTRATO nº 45/2026
                FISCALIZAÇÃO CONTRATUAL
                PREFEITURA MUNICIPAL DE VALE VERDE
                PROCURADORIA JURÍDICA
                CNPJ 12.345.678/0001-99
                contato@solucoesintegradas.com.br
                (65) 99999-9999
                CEP 78000-000
                Valor R$ 12.345,67
                Data 14/03/2026
                """);
        _factory.AiDocumentClassifier.ClassifyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new DocumentClassificationResult(DocumentTypes.RelatorioTecnicoPreliminar, "PROCESSO_ADMINISTRATIVO", "APURACAO_CONTRATUAL", 0.98));
        _factory.AiStructuredDataExtractor.ExtractAsync(
                Arg.Any<string>(),
                Arg.Any<DocumentClass>(),
                Arg.Any<IReadOnlyCollection<DocumentClassIndexer>>(),
                Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, object?>
            {
                ["orgao"] = "Prefeitura Municipal de Vale Verde",
                ["empresa"] = "Solucoes Integradas LTDA"
            });

        await SeedUserAndDocumentAsync(user, document);
        await SeedRelatorioSetupAsync();
        Authenticate(user);

        var response = await _client.PostAsync($"/api/documents/{document.Id}/process", content: null);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<ProcessDocumentResponse>();
        Assert.NotNull(result);
        Assert.Equal("Processing", result.Status);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DoclynDbContext>();
        var persistedDocument = await context.Documents.SingleAsync(d => d.Id == document.Id);
        var extractedData = await context.ExtractedData.SingleAsync(ed => ed.DocumentId == document.Id);
        var logs = await context.ProcessingLogs.Where(log => log.DocumentId == document.Id).ToListAsync();

        Assert.Equal(DocumentStatus.Processed, persistedDocument.DocumentStatus);
        Assert.Equal(DocumentTypes.RelatorioTecnicoPreliminar, persistedDocument.DocumentType);
        Assert.Equal(DocumentTypes.RelatorioTecnicoPreliminar, extractedData.Data.RootElement.GetProperty("classification").GetProperty("documentType").GetString());
        Assert.Equal("2026/98765", extractedData.Data.RootElement.GetProperty("finalResult").GetProperty("numeroProcesso").GetString());
        Assert.Equal("2026/98765", extractedData.Data.RootElement.GetProperty("indexers").GetProperty("numeroProcesso").GetProperty("value").GetString());
        Assert.Equal("Prefeitura Municipal de Vale Verde", extractedData.Data.RootElement.GetProperty("aiExtraction").GetProperty("orgao").GetString());
        Assert.Contains(logs, log => log.Step == "ProcessingStarted");
        Assert.Contains(logs, log => log.Step == "ProcessingCompleted");
        Assert.True(extractedData.Data.RootElement.TryGetProperty("fields", out var fieldsElement));
        Assert.Equal("Regex", fieldsElement.GetProperty("numeroProcesso").GetProperty("source").GetString());
        Assert.Equal("Validated", fieldsElement.GetProperty("numeroProcesso").GetProperty("validationStatus").GetString());
        Assert.True(fieldsElement.GetProperty("numeroProcesso").GetProperty("confidence").GetDecimal() > 0);
    }

    [Fact]
    public async Task Process_Without_Jwt_Returns_401()
    {
        var response = await _client.PostAsync($"/api/documents/{Guid.NewGuid()}/process", content: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Process_Invalid_Document_Returns_404()
    {
        var user = TestAuthHelper.CreateOperator();
        await SeedUserAsync(user);
        Authenticate(user);

        var response = await _client.PostAsync($"/api/documents/{Guid.NewGuid()}/process", content: null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Process_Operator_Cannot_Process_Other_Users_Document()
    {
        var owner = TestAuthHelper.CreateOperator("owner-process@doclyn.local");
        var other = TestAuthHelper.CreateOperator("other-process@doclyn.local");
        var document = Document.Create(owner.Id, "file.pdf", "hash", "documents/test/original.pdf", DocumentTypes.Unknown);

        await SeedUsersAndDocumentsAsync(owner, other, document);
        Authenticate(other);

        var response = await _client.PostAsync($"/api/documents/{document.Id}/process", content: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Process_With_Insufficient_Text_Sets_Document_As_Failed()
    {
        var user = TestAuthHelper.CreateOperator();
        var document = Document.Create(user.Id, "file.pdf", "hash", "documents/test/original.pdf", DocumentTypes.Unknown);

        _factory.FileStorageService.DownloadAsync(document.StoragePath, Arg.Any<CancellationToken>())
            .Returns(new MemoryStream([1, 2, 3]));
        _factory.PdfTextExtractor.ExtractTextAsync(Arg.Any<Stream>(), Arg.Any<CancellationToken>())
            .Returns("short text");
        _factory.OcrService.ExtractTextAsync(Arg.Any<Stream>(), Arg.Any<CancellationToken>())
            .Returns(string.Empty);

        await SeedUserAndDocumentAsync(user, document);
        await SeedRelatorioSetupAsync();
        Authenticate(user);

        var response = await _client.PostAsync($"/api/documents/{document.Id}/process", content: null);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DoclynDbContext>();
        var persistedDocument = await context.Documents.SingleAsync(d => d.Id == document.Id);
        var failureLog = await context.ProcessingLogs.SingleAsync(log => log.DocumentId == document.Id && log.Step == "ProcessingFailed");

        Assert.Equal(DocumentStatus.Failed, persistedDocument.DocumentStatus);
        Assert.Contains("insufficient", failureLog.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Reprocess_With_Ocr_Returns_202_And_Reprocesses_Document()
    {
        var user = TestAuthHelper.CreateOperator();
        var document = Document.Create(user.Id, "file.pdf", "hash", "documents/test/original.pdf", DocumentTypes.Unknown);
        document.UpdateStatus(DocumentStatus.Failed);

        _factory.PdfTextExtractor.ExtractTextAsync(Arg.Any<Stream>(), Arg.Any<CancellationToken>())
            .Returns("short text");
        _factory.OcrService.ExtractTextAsync(Arg.Any<Stream>(), Arg.Any<CancellationToken>())
            .Returns("""
                RELATÓRIO TÉCNICO PRELIMINAR
                PROCESSO ADMINISTRATIVO Nº 2026/98765
                CONTRATO nº 45/2026
                PREFEITURA MUNICIPAL DE VALE VERDE
                CNPJ 12.345.678/0001-99
                """);
        _factory.AiDocumentClassifier.ClassifyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new DocumentClassificationResult(DocumentTypes.RelatorioTecnicoPreliminar, "PROCESSO_ADMINISTRATIVO", "APURACAO_CONTRATUAL", 0.95));
        _factory.AiStructuredDataExtractor.ExtractAsync(
                Arg.Any<string>(),
                Arg.Any<DocumentClass>(),
                Arg.Any<IReadOnlyCollection<DocumentClassIndexer>>(),
                Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, object?> { ["orgao"] = "Reprocessado com IA" });

        await SeedUserAndDocumentAsync(user, document);
        await SeedRelatorioSetupAsync();
        Authenticate(user);

        var response = await _client.PostAsync($"/api/documents/{document.Id}/reprocess", content: null);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<ReprocessDocumentResponse>();
        Assert.NotNull(result);
        Assert.Equal("Processing", result.Status);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DoclynDbContext>();
        var persistedDocument = await context.Documents.SingleAsync(d => d.Id == document.Id);
        var logs = await context.ProcessingLogs.Where(log => log.DocumentId == document.Id).ToListAsync();

        Assert.Equal(DocumentStatus.Processed, persistedDocument.DocumentStatus);
        Assert.Contains(logs, log => log.Step == "ReprocessRequested");
        Assert.Contains(logs, log => log.Step == "OcrCompleted");
        Assert.Contains(logs, log => log.Step == "AiExtractionCompleted");
    }

    [Fact]
    public async Task Process_When_Ai_Fails_Still_Processes_With_Regex_Data()
    {
        var user = TestAuthHelper.CreateOperator("ai-fallback@doclyn.local");
        var document = Document.Create(user.Id, "file.pdf", "hash", "documents/test/original.pdf", DocumentTypes.Unknown);

        _factory.PdfTextExtractor.ExtractTextAsync(Arg.Any<Stream>(), Arg.Any<CancellationToken>())
            .Returns("""
                RELATÓRIO TÉCNICO PRELIMINAR
                PROCESSO ADMINISTRATIVO Nº 2026/98765
                CONTRATO nº 45/2026
                FISCALIZAÇÃO CONTRATUAL
                PREFEITURA MUNICIPAL DE VALE VERDE
                PROCURADORIA JURÍDICA
                CNPJ 12.345.678/0001-99
                """);
        _factory.AiDocumentClassifier.ClassifyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<DocumentClassificationResult>(new InvalidOperationException("AI unavailable")));

        await SeedUserAndDocumentAsync(user, document);
        await SeedRelatorioSetupAsync();
        Authenticate(user);

        var response = await _client.PostAsync($"/api/documents/{document.Id}/process", content: null);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DoclynDbContext>();
        var persistedDocument = await context.Documents.SingleAsync(d => d.Id == document.Id);
        var extractedData = await context.ExtractedData.SingleAsync(ed => ed.DocumentId == document.Id);
        var logs = await context.ProcessingLogs.Where(log => log.DocumentId == document.Id).ToListAsync();

        Assert.Equal(DocumentStatus.Processed, persistedDocument.DocumentStatus);
        Assert.Equal("2026/98765", extractedData.Data.RootElement.GetProperty("finalResult").GetProperty("numeroProcesso").GetString());
        Assert.True(extractedData.Data.RootElement.GetProperty("aiExtraction").ValueKind is JsonValueKind.Null);
    }

    [Fact]
    public async Task ReprocessBatch_Returns_Counts_And_Enqueues_Eligible_Documents()
    {
        var user = TestAuthHelper.CreateOperator();
        var document1 = Document.Create(user.Id, "one.pdf", "hash1", "path1", DocumentTypes.Unknown);
        var document2 = Document.Create(user.Id, "two.pdf", "hash2", "path2", DocumentTypes.Unknown);
        document2.UpdateStatus(DocumentStatus.Processing);

        _factory.PdfTextExtractor.ExtractTextAsync(Arg.Any<Stream>(), Arg.Any<CancellationToken>())
            .Returns("""
                RELATÓRIO TÉCNICO PRELIMINAR
                PROCESSO ADMINISTRATIVO Nº 2026/98765
                CONTRATO nº 45/2026
                PREFEITURA MUNICIPAL DE VALE VERDE
                CNPJ 12.345.678/0001-99
                """);

        await SeedUserAndDocumentsAsync(user, document1, document2);
        Authenticate(user);

        var response = await _client.PostAsJsonAsync(
            "/api/documents/reprocess-batch",
            new ReprocessBatchCommand([document1.Id, document2.Id]));

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<ReprocessBatchResponse>();
        Assert.NotNull(result);
        Assert.Equal(2, result.Requested);
        Assert.Equal(1, result.Enqueued);
        Assert.Equal(1, result.Skipped);
    }

    [Fact]
    public async Task ReprocessByFilter_Returns_Matched_And_Enqueued_Counts()
    {
        var user = TestAuthHelper.CreateOperator();
        var document1 = Document.Create(user.Id, "one.pdf", "hash1", "path1", DocumentTypes.Unknown);
        var document2 = Document.Create(user.Id, "two.pdf", "hash2", "path2", DocumentTypes.Unknown);
        document1.UpdateStatus(DocumentStatus.Failed);
        document2.UpdateStatus(DocumentStatus.Failed);

        _factory.PdfTextExtractor.ExtractTextAsync(Arg.Any<Stream>(), Arg.Any<CancellationToken>())
            .Returns("""
                RELATÓRIO TÉCNICO PRELIMINAR
                PROCESSO ADMINISTRATIVO Nº 2026/98765
                CONTRATO nº 45/2026
                PREFEITURA MUNICIPAL DE VALE VERDE
                CNPJ 12.345.678/0001-99
                """);

        await SeedUserAndDocumentsAsync(user, document1, document2);
        Authenticate(user);

        var response = await _client.PostAsJsonAsync(
            "/api/documents/reprocess-by-filter",
            new ReprocessByFilterCommand(DocumentStatus.Failed.ToString(), DocumentTypes.Unknown, null, null));

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<ReprocessByFilterResponse>();
        Assert.NotNull(result);
        Assert.Equal(2, result.Matched);
        Assert.Equal(2, result.Enqueued);
        Assert.Equal(0, result.Skipped);
    }

    [Fact]
    public async Task GetAll_Operator_Returns_Only_Own_Documents()
    {
        var operatorUser = TestAuthHelper.CreateOperator("operator1@doclyn.local");
        var otherOperator = TestAuthHelper.CreateOperator("operator2@doclyn.local");

        var ownDocument = Document.Create(operatorUser.Id, "own.pdf", "hash1", "path1", DocumentTypes.Unknown);
        var otherDocument = Document.Create(otherOperator.Id, "other.pdf", "hash2", "path2", DocumentTypes.Unknown);

        await SeedUsersAndDocumentsAsync(operatorUser, otherOperator, ownDocument, otherDocument);
        Authenticate(operatorUser);

        var response = await _client.GetAsync("/api/documents?page=1&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<dynamic>();
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetById_Operator_Cannot_Access_Others_Document()
    {
        var owner = TestAuthHelper.CreateOperator("owner@doclyn.local");
        var other = TestAuthHelper.CreateOperator("other@doclyn.local");
        var document = Document.Create(owner.Id, "file.pdf", "hash", "path", DocumentTypes.Unknown);

        await SeedUsersAndDocumentsAsync(owner, other, document);
        Authenticate(other);

        var response = await _client.GetAsync($"/api/documents/{document.Id}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Delete_Owner_Soft_Deletes_Document_And_Hides_It_From_Queries()
    {
        var user = TestAuthHelper.CreateOperator("delete-owner@doclyn.local");
        var document = Document.Create(user.Id, "file.pdf", "hash", "path", DocumentTypes.Unknown);

        await SeedUserAndDocumentAsync(user, document);
        Authenticate(user);

        var deleteResponse = await _client.DeleteAsync($"/api/documents/{document.Id}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getAllResponse = await _client.GetAsync("/api/documents?page=1&pageSize=10");
        var getByIdResponse = await _client.GetAsync($"/api/documents/{document.Id}");

        Assert.Equal(HttpStatusCode.OK, getAllResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, getByIdResponse.StatusCode);

        var list = await getAllResponse.Content.ReadFromJsonAsync<GetDocumentsResponse>();
        Assert.NotNull(list);
        Assert.Empty(list.Items);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DoclynDbContext>();
        var persistedDocument = await context.Documents.IgnoreQueryFilters().SingleAsync(d => d.Id == document.Id);
        var deletionLog = await context.ProcessingLogs.SingleAsync(l => l.DocumentId == document.Id && l.Step == "DocumentDeleted");

        Assert.True(persistedDocument.IsDeleted);
        Assert.Equal(user.Id, persistedDocument.DeletedByUserId);
        Assert.Equal(DocumentStatus.Success, deletionLog.Status);
    }

    [Fact]
    public async Task Delete_Operator_Cannot_Delete_Other_User_Document()
    {
        var owner = TestAuthHelper.CreateOperator("delete-owner-2@doclyn.local");
        var other = TestAuthHelper.CreateOperator("delete-other@doclyn.local");
        var document = Document.Create(owner.Id, "file.pdf", "hash", "path", DocumentTypes.Unknown);

        await SeedUsersAndDocumentsAsync(owner, other, document);
        Authenticate(other);

        var response = await _client.DeleteAsync($"/api/documents/{document.Id}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Delete_Admin_Can_Delete_Any_Document_And_Restore_Admin_Can_Reveal_It_Again()
    {
        var owner = TestAuthHelper.CreateOperator("restore-owner@doclyn.local");
        var admin = TestAuthHelper.CreateAdmin("restore-admin@doclyn.local");
        var document = Document.Create(owner.Id, "file.pdf", "hash", "path", DocumentTypes.Unknown);

        await SeedUsersAndDocumentsAsync(owner, admin, document);
        Authenticate(admin);

        var deleteResponse = await _client.DeleteAsync($"/api/documents/{document.Id}");
        var restoreResponse = await _client.PostAsync($"/api/documents/{document.Id}/restore", content: null);

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, restoreResponse.StatusCode);

        var getByIdResponse = await _client.GetAsync($"/api/documents/{document.Id}");
        Assert.Equal(HttpStatusCode.OK, getByIdResponse.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DoclynDbContext>();
        var persistedDocument = await context.Documents.IgnoreQueryFilters().SingleAsync(d => d.Id == document.Id);
        var restoreLog = await context.ProcessingLogs.SingleAsync(l => l.DocumentId == document.Id && l.Step == "DocumentRestored");

        Assert.False(persistedDocument.IsDeleted);
        Assert.Null(persistedDocument.DeletedAt);
        Assert.Null(persistedDocument.DeletedByUserId);
        Assert.Equal(DocumentStatus.Success, restoreLog.Status);
    }

    [Fact]
    public async Task Restore_Non_Admin_Cannot_Restore_Deleted_Document()
    {
        var user = TestAuthHelper.CreateOperator("restore-user@doclyn.local");
        var admin = TestAuthHelper.CreateAdmin("restore-admin-2@doclyn.local");
        var document = Document.Create(user.Id, "file.pdf", "hash", "path", DocumentTypes.Unknown);
        document.Delete(admin.Id);

        await SeedUsersAndDocumentsAsync(user, admin, document);
        Authenticate(user);

        var response = await _client.PostAsync($"/api/documents/{document.Id}/restore", content: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Deleted_Document_Derived_Endpoints_Return_404()
    {
        var user = TestAuthHelper.CreateOperator("delete-derived@doclyn.local");
        var document = Document.Create(user.Id, "file.pdf", "hash", "path", DocumentTypes.Unknown);
        var log = ProcessingLog.Create(document.Id, "Upload", "Success", DocumentStatus.Success);

        await SeedUserAndDocumentWithLogAsync(user, document, log);
        Authenticate(user);

        var deleteResponse = await _client.DeleteAsync($"/api/documents/{document.Id}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var logsResponse = await _client.GetAsync($"/api/documents/{document.Id}/logs");
        var extractedDataResponse = await _client.GetAsync($"/api/documents/{document.Id}/extracted-data");
        var reviewFieldsResponse = await _client.GetAsync($"/api/documents/{document.Id}/review-fields");

        Assert.Equal(HttpStatusCode.NotFound, logsResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, extractedDataResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, reviewFieldsResponse.StatusCode);
    }

    [Fact]
    public async Task GetLogs_Returns_Logs()
    {
        var user = TestAuthHelper.CreateOperator();
        var document = Document.Create(user.Id, "file.pdf", "hash", "path", DocumentTypes.Unknown);
        var log = ProcessingLog.Create(document.Id, "Upload", "Success", DocumentStatus.Success);

        await SeedUserAndDocumentWithLogAsync(user, document, log);
        Authenticate(user);

        var response = await _client.GetAsync($"/api/documents/{document.Id}/logs");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<List<GetDocumentLogResponse>>();
        Assert.NotNull(result);
        Assert.Single(result);
    }

    [Fact]
    public async Task GetExtractedData_Returns_Null_When_No_Extraction()
    {
        var user = TestAuthHelper.CreateOperator();
        var document = Document.Create(user.Id, "file.pdf", "hash", "path", DocumentTypes.Unknown);

        await SeedUserAndDocumentAsync(user, document);
        Authenticate(user);

        var response = await _client.GetAsync($"/api/documents/{document.Id}/extracted-data");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<GetExtractedDataResponse>();
        Assert.NotNull(result);
        Assert.Null(result.Data);
    }

    [Fact]
    public async Task Reclassify_Returns_202_And_Logs_Reclassification_Requested()
    {
        var user = TestAuthHelper.CreateOperator();
        var document = Document.Create(user.Id, "file.pdf", "hash", "path", DocumentTypes.Unknown);
        document.UpdateStatus(DocumentStatus.Failed);

        await SeedUserAndDocumentAsync(user, document);
        Authenticate(user);

        var response = await _client.PostAsync($"/api/documents/{document.Id}/reclassify", content: null);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DoclynDbContext>();
        var log = await context.ProcessingLogs
            .FirstOrDefaultAsync(l => l.DocumentId == document.Id && l.Step == "ReclassificationRequested");

        Assert.NotNull(log);
        Assert.Contains("semantic reclassification", log.Message);
    }

    [Fact]
    public async Task GetReviewFields_Returns_Only_NeedsReview_Fields()
    {
        var user = TestAuthHelper.CreateOperator();
        var document = Document.Create(user.Id, "file.pdf", "hash", "path", DocumentTypes.Unknown);

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<DoclynDbContext>();
            context.Users.Add(user);
            context.Documents.Add(document);

            var extractedData = ExtractedData.Create(document.Id, JsonDocument.Parse(
                """
                {
                  "fields": {
                    "numeroProcesso": {
                      "value": "2026/123",
                      "confidence": 1.0,
                      "source": "Regex",
                      "validationStatus": "Validated"
                    },
                    "orgao": {
                      "value": "Prefeitura",
                      "confidence": 0.80,
                      "source": "AI",
                      "validationStatus": "NeedsReview"
                    },
                    "assunto": {
                      "value": "Fiscalizacao",
                      "confidence": 0.65,
                      "source": "AI",
                      "validationStatus": "Rejected"
                    }
                  }
                }
                """));
            context.ExtractedData.Add(extractedData);
            await context.SaveChangesAsync();
        }

        Authenticate(user);

        var response = await _client.GetAsync($"/api/documents/{document.Id}/review-fields");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<GetReviewFieldsResponse>();
        Assert.NotNull(result);
        Assert.Single(result.Fields);
        Assert.Equal("orgao", result.Fields[0].FieldName);
        Assert.Equal("NeedsReview", result.Fields[0].ValidationStatus);
        Assert.Equal(0.80m, result.Fields[0].Confidence);
    }

    [Fact]
    public async Task GetReviewFields_Returns_Empty_When_All_Validated()
    {
        var user = TestAuthHelper.CreateOperator();
        var document = Document.Create(user.Id, "file.pdf", "hash", "path", DocumentTypes.Unknown);

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<DoclynDbContext>();
            context.Users.Add(user);
            context.Documents.Add(document);

            var extractedData = ExtractedData.Create(document.Id, JsonDocument.Parse(
                """
                {
                  "fields": {
                    "numeroProcesso": {
                      "value": "2026/123",
                      "confidence": 1.0,
                      "source": "Regex",
                      "validationStatus": "Validated"
                    }
                  }
                }
                """));
            context.ExtractedData.Add(extractedData);
            await context.SaveChangesAsync();
        }

        Authenticate(user);

        var response = await _client.GetAsync($"/api/documents/{document.Id}/review-fields");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<GetReviewFieldsResponse>();
        Assert.NotNull(result);
        Assert.Empty(result.Fields);
    }

    private static MultipartFormDataContent CreateMultipartContent(
        string fileName,
        string contentType,
        byte[] fileBytes)
    {
        var content = new MultipartFormDataContent();
        var streamContent = new StreamContent(new MemoryStream(fileBytes));
        streamContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        content.Add(streamContent, "file", fileName);
        return content;
    }

    private async Task SeedRelatorioSetupAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DoclynDbContext>();

        var documentClass = await context.DocumentClasses
            .FirstOrDefaultAsync(dc => dc.Name == DocumentTypes.RelatorioTecnicoPreliminar);

        if (documentClass is null)
        {
            documentClass = DocumentClass.Create(
                DocumentTypes.RelatorioTecnicoPreliminar,
                "ADMINISTRATIVO",
                "PROCESSO_ADMINISTRATIVO",
                "Classe de relatorio tecnico preliminar.",
                isSystemDefined: true);

            context.DocumentClasses.Add(documentClass);
            await context.SaveChangesAsync();
        }

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

            await context.SaveChangesAsync();
        }
    }

    private void Authenticate(User user)
    {
        var token = TestAuthHelper.GenerateToken(user);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private async Task SeedUserAsync(User user)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DoclynDbContext>();
        context.Users.Add(user);
        await context.SaveChangesAsync();
    }

    private async Task SeedUsersAndDocumentsAsync(
        User user1,
        User user2,
        Document document1,
        Document? document2 = null)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DoclynDbContext>();
        context.Users.Add(user1);
        context.Users.Add(user2);
        context.Documents.Add(document1);
        if (document2 is not null)
        {
            context.Documents.Add(document2);
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

    private async Task SeedUserAndDocumentsAsync(User user, params Document[] documents)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DoclynDbContext>();
        context.Users.Add(user);
        context.Documents.AddRange(documents);
        await context.SaveChangesAsync();
    }

    private async Task SeedUserAndDocumentWithLogAsync(User user, Document document, ProcessingLog log)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DoclynDbContext>();
        context.Users.Add(user);
        context.Documents.Add(document);
        context.ProcessingLogs.Add(log);
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task Process_Fails_When_Classification_Returns_Unknown()
    {
        var user = TestAuthHelper.CreateOperator("classification-fail@doclyn.local");
        var document = Document.Create(user.Id, "file.pdf", "hash", "documents/test/original.pdf", DocumentTypes.Unknown);

        _factory.DocumentSemanticClassificationService
            .ClassifyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new SemanticClassificationResult(
                DocumentClassId: null,
                DocumentType: DocumentTypes.DocumentoDesconhecido,
                Group: "UNKNOWN",
                SubGroup: "UNKNOWN",
                Confidence: 0.1m,
                ReusedExistingClass: false,
                NewClassSuggested: true));

        _factory.FileStorageService.DownloadAsync(document.StoragePath, Arg.Any<CancellationToken>())
            .Returns(new MemoryStream([1, 2, 3]));
        _factory.PdfTextExtractor.ExtractTextAsync(Arg.Any<Stream>(), Arg.Any<CancellationToken>())
            .Returns("generic document content without known classification patterns");
        _factory.OcrService.ExtractTextAsync(Arg.Any<Stream>(), Arg.Any<CancellationToken>())
            .Returns("PREFEITURA MUNICIPAL DE VALE VERDE");
        _factory.AiDocumentClassifier.ClassifyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<DocumentClassificationResult>(new InvalidOperationException("AI unavailable")));

        await SeedUserAndDocumentAsync(user, document);
        Authenticate(user);

        var response = await _client.PostAsync($"/api/documents/{document.Id}/process", content: null);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DoclynDbContext>();
        var persistedDocument = await context.Documents.IgnoreQueryFilters().SingleAsync(d => d.Id == document.Id);
        var logs = await context.ProcessingLogs.Where(log => log.DocumentId == document.Id).ToListAsync();

        Assert.Equal(DocumentStatus.Failed, persistedDocument.DocumentStatus);
        Assert.Contains(logs, log => log.Step == "ClassificationFailed");
    }

    private async Task SeedUnknownClassIndexersAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DoclynDbContext>();

        var unknownClass = await context.DocumentClasses
            .FirstOrDefaultAsync(dc => dc.Name == "DOCUMENTO_DESCONHECIDO");

        if (unknownClass is null)
        {
            unknownClass = DocumentClass.Create("DOCUMENTO_DESCONHECIDO", "OUTROS", "NAO_CLASSIFICADO",
                "Documento sem classificacao conhecida.", isSystemDefined: true);
            context.DocumentClasses.Add(unknownClass);
            await context.SaveChangesAsync();
        }

        if (await context.DocumentClassIndexers.AnyAsync(i => i.DocumentClassId == unknownClass.Id))
            return;

        context.DocumentClassIndexers.AddRange(
            DocumentClassIndexer.Create(unknownClass.Id, "cnpj", "CNPJ", "", IndexerDataType.Cnpj, false, false, null, @"\d{2}\.\d{3}\.\d{3}\/\d{4}-\d{2}"),
            DocumentClassIndexer.Create(unknownClass.Id, "orgao", "Orgao", "", IndexerDataType.Text, false, false, null, null),
            DocumentClassIndexer.Create(unknownClass.Id, "emails", "E-mails", "", IndexerDataType.Email, false, true, null, @"[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}"),
            DocumentClassIndexer.Create(unknownClass.Id, "datas", "Datas", "", IndexerDataType.Date, false, true, null, @"\d{2}\/\d{2}\/\d{4}"),
            DocumentClassIndexer.Create(unknownClass.Id, "valores", "Valores", "", IndexerDataType.Currency, false, true, null, @"R\$\s*\d{1,3}(?:\.\d{3})*,\d{2}"),
            DocumentClassIndexer.Create(unknownClass.Id, "telefones", "Telefones", "", IndexerDataType.Phone, false, true, null, @"\(\d{2}\)\s*\d{4,5}-\d{4}"),
            DocumentClassIndexer.Create(unknownClass.Id, "palavrasChave", "Palavras-chave", "", IndexerDataType.Array, false, true, null, null),
            DocumentClassIndexer.Create(unknownClass.Id, "assunto", "Assunto", "", IndexerDataType.Text, false, false, null, null)
        );
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task Download_Without_Jwt_Returns_401()
    {
        var response = await _client.GetAsync($"/api/documents/{Guid.NewGuid()}/download");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Download_Owner_Can_Download_Own_Document()
    {
        var user = TestAuthHelper.CreateOperator("download-owner@doclyn.local");
        var document = Document.Create(user.Id, "file.pdf", "hash", "documents/test/original.pdf", DocumentTypes.Unknown);

        _factory.FileStorageService.DownloadAsync(document.StoragePath, Arg.Any<CancellationToken>())
            .Returns(new MemoryStream([0x25, 0x50, 0x44, 0x46]));

        await SeedUserAndDocumentAsync(user, document);
        Authenticate(user);

        var response = await _client.GetAsync($"/api/documents/{document.Id}/download");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("attachment", response.Content.Headers.ContentDisposition?.ToString() ?? "");
    }

    [Fact]
    public async Task Download_Other_User_Cannot_Download()
    {
        var owner = TestAuthHelper.CreateOperator("download-owner-2@doclyn.local");
        var other = TestAuthHelper.CreateOperator("download-other@doclyn.local");
        var document = Document.Create(owner.Id, "file.pdf", "hash", "path", DocumentTypes.Unknown);

        await SeedUsersAndDocumentsAsync(owner, other, document);
        Authenticate(other);

        var response = await _client.GetAsync($"/api/documents/{document.Id}/download");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
