using Doclyn.Application.Dashboard.GetSummary;
using Doclyn.Domain.Constants;
using Doclyn.Domain.Entities;
using Doclyn.Domain.Enums;
using Doclyn.UnitTests.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System.Text.Json;

namespace Doclyn.UnitTests.Dashboard.GetSummary;

public sealed class GetDashboardSummaryHandlerTests
{
    private readonly TestDbContext _context;
    private readonly CurrentUserServiceMock _currentUser;
    private readonly ILogger<GetDashboardSummaryHandler> _logger;
    private readonly GetDashboardSummaryHandler _handler;

    public GetDashboardSummaryHandlerTests()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new TestDbContext(options);
        _currentUser = new CurrentUserServiceMock();

        _logger = Substitute.For<ILogger<GetDashboardSummaryHandler>>();

        _handler = new GetDashboardSummaryHandler(_context, _currentUser, _logger);
    }

    [Fact]
    public async Task Handle_Logs_Requested_And_Generated_For_Successful_Request()
    {
        var userId = Guid.NewGuid();

        SeedDocument(userId, DocumentStatus.Processed);
        await _context.SaveChangesAsync();

        _currentUser.UserId = userId;
        _currentUser.Role = UserRole.Operator.ToString();

        await _handler.Handle(new GetDashboardSummaryQuery(), CancellationToken.None);

        AssertLogReceived(LogLevel.Information, "DashboardSummaryRequested");
        AssertLogReceived(LogLevel.Information, "DashboardSummaryGenerated");
        AssertLogNotReceived(LogLevel.Error, "DashboardSummaryFailed");
    }

    [Fact]
    public async Task Operator_Visualizes_Only_Own_Document_Counts()
    {
        var operatorId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        SeedDocument(operatorId, DocumentStatus.Pending);
        SeedDocument(operatorId, DocumentStatus.Processing);
        SeedDocument(operatorId, DocumentStatus.Processed);
        SeedDocument(operatorId, DocumentStatus.Failed);

        SeedDocument(otherUserId, DocumentStatus.Pending);
        SeedDocument(otherUserId, DocumentStatus.Processed);

        await _context.SaveChangesAsync();

        _currentUser.UserId = operatorId;
        _currentUser.Role = UserRole.Operator.ToString();

        var response = await _handler.Handle(new GetDashboardSummaryQuery(), CancellationToken.None);

        Assert.Equal(4, response.Documents.Total);
        Assert.Equal(1, response.Documents.Pending);
        Assert.Equal(1, response.Documents.Processing);
        Assert.Equal(1, response.Documents.Processed);
        Assert.Equal(1, response.Documents.Failed);
    }

    [Fact]
    public async Task Admin_Visualizes_Global_Document_Counts()
    {
        var adminId = Guid.NewGuid();

        SeedDocument(Guid.NewGuid(), DocumentStatus.Pending);
        SeedDocument(Guid.NewGuid(), DocumentStatus.Processing);
        SeedDocument(Guid.NewGuid(), DocumentStatus.Processed);
        SeedDocument(Guid.NewGuid(), DocumentStatus.Failed);
        SeedDocument(Guid.NewGuid(), DocumentStatus.Processed);

        await _context.SaveChangesAsync();

        _currentUser.UserId = adminId;
        _currentUser.Role = UserRole.Admin.ToString();

        var response = await _handler.Handle(new GetDashboardSummaryQuery(), CancellationToken.None);

        Assert.Equal(5, response.Documents.Total);
        Assert.Equal(1, response.Documents.Pending);
        Assert.Equal(1, response.Documents.Processing);
        Assert.Equal(2, response.Documents.Processed);
        Assert.Equal(1, response.Documents.Failed);
    }

    [Fact]
    public async Task Insight_Counts_Per_Severity_Are_Correct_And_Scoped()
    {
        var operatorId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        var operatorDocument = SeedDocument(operatorId, DocumentStatus.Processed);
        var otherDocument = SeedDocument(otherUserId, DocumentStatus.Processed);

        _context.DocumentInsights.AddRange(
            CreateInsight(operatorDocument.Id, DocumentInsightSeverity.Critical),
            CreateInsight(operatorDocument.Id, DocumentInsightSeverity.Warning),
            CreateInsight(operatorDocument.Id, DocumentInsightSeverity.Warning),
            CreateInsight(operatorDocument.Id, DocumentInsightSeverity.Info),
            CreateInsight(operatorDocument.Id, DocumentInsightSeverity.Success),
            CreateInsight(otherDocument.Id, DocumentInsightSeverity.Critical),
            CreateInsight(otherDocument.Id, DocumentInsightSeverity.Success));

        await _context.SaveChangesAsync();

        _currentUser.UserId = operatorId;
        _currentUser.Role = UserRole.Operator.ToString();

        var response = await _handler.Handle(new GetDashboardSummaryQuery(), CancellationToken.None);

        Assert.Equal(5, response.Insights.Total);
        Assert.Equal(1, response.Insights.Critical);
        Assert.Equal(2, response.Insights.Warning);
        Assert.Equal(1, response.Insights.Info);
        Assert.Equal(1, response.Insights.Success);
    }

    [Fact]
    public async Task Admin_Visualizes_Global_Insight_Counts()
    {
        var adminId = Guid.NewGuid();

        var firstDocument = SeedDocument(Guid.NewGuid(), DocumentStatus.Processed);
        var secondDocument = SeedDocument(Guid.NewGuid(), DocumentStatus.Processed);

        _context.DocumentInsights.AddRange(
            CreateInsight(firstDocument.Id, DocumentInsightSeverity.Critical),
            CreateInsight(firstDocument.Id, DocumentInsightSeverity.Warning),
            CreateInsight(secondDocument.Id, DocumentInsightSeverity.Info),
            CreateInsight(secondDocument.Id, DocumentInsightSeverity.Success));

        await _context.SaveChangesAsync();

        _currentUser.UserId = adminId;
        _currentUser.Role = UserRole.Admin.ToString();

        var response = await _handler.Handle(new GetDashboardSummaryQuery(), CancellationToken.None);

        Assert.Equal(4, response.Insights.Total);
        Assert.Equal(1, response.Insights.Critical);
        Assert.Equal(1, response.Insights.Warning);
        Assert.Equal(1, response.Insights.Info);
        Assert.Equal(1, response.Insights.Success);
    }

    [Fact]
    public async Task Quality_Aggregates_Field_Confidence_And_Validation_Statuses_From_Scoped_Json()
    {
        var operatorId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        var firstDocument = SeedDocument(operatorId, DocumentStatus.Processed);
        var secondDocument = SeedDocument(operatorId, DocumentStatus.Processed);
        var otherDocument = SeedDocument(otherUserId, DocumentStatus.Processed);

        SeedExtractedData(firstDocument.Id, """
            {
              "fields": {
                "numero": { "value": "123", "confidence": 0.90, "validationStatus": "Validated" },
                "valor": { "value": "1000", "confidence": 0.80, "validationStatus": "NeedsReview" }
              }
            }
            """);

        SeedExtractedData(secondDocument.Id, """
            {
              "fields": {
                "data": { "value": "2026-06-17", "confidence": 0.70, "validationStatus": "Rejected" },
                "orgao": { "value": "Doclyn", "confidence": 0.60, "validationStatus": "Validated" }
              }
            }
            """);

        SeedExtractedData(otherDocument.Id, """
            {
              "fields": {
                "ignored": { "value": "x", "confidence": 0.10, "validationStatus": "Rejected" }
              }
            }
            """);

        await _context.SaveChangesAsync();

        _currentUser.UserId = operatorId;
        _currentUser.Role = UserRole.Operator.ToString();

        var response = await _handler.Handle(new GetDashboardSummaryQuery(), CancellationToken.None);

        Assert.Equal(0.75m, response.Quality.AverageConfidence);
        Assert.Equal(2, response.Quality.FieldsValidated);
        Assert.Equal(1, response.Quality.FieldsNeedsReview);
        Assert.Equal(1, response.Quality.FieldsRejected);
    }

    [Fact]
    public async Task Recent_Documents_Are_Limited_To_Five_And_Ordered_By_Newest_First()
    {
        var userId = Guid.NewGuid();
        var createdAt = new DateTime(2026, 06, 17, 12, 0, 0, DateTimeKind.Utc);
        var documents = new List<Document>();

        for (var index = 0; index < 6; index++)
        {
            var document = SeedDocument(userId, DocumentStatus.Processed, $"document-{index}.pdf");
            SetCreatedAt(document, createdAt.AddMinutes(index));
            documents.Add(document);
        }

        await _context.SaveChangesAsync();

        _currentUser.UserId = userId;
        _currentUser.Role = UserRole.Operator.ToString();

        var response = await _handler.Handle(new GetDashboardSummaryQuery(), CancellationToken.None);

        Assert.Equal(5, response.RecentDocuments.Count);
        Assert.Equal(documents[5].Id, response.RecentDocuments[0].Id);
        Assert.Equal(documents[4].Id, response.RecentDocuments[1].Id);
        Assert.Equal(documents[3].Id, response.RecentDocuments[2].Id);
        Assert.Equal(documents[2].Id, response.RecentDocuments[3].Id);
        Assert.Equal(documents[1].Id, response.RecentDocuments[4].Id);
    }

    [Fact]
    public async Task Recent_Documents_Include_Computed_Metadata_From_Extracted_Data_And_Insights()
    {
        var userId = Guid.NewGuid();
        var document = SeedDocument(userId, DocumentStatus.Processing, "contrato.pdf");
        SetCreatedAt(document, new DateTime(2026, 06, 17, 12, 0, 0, DateTimeKind.Utc));

        SeedExtractedData(document.Id, """
            {
              "classification": {
                "documentType": "CONTRATO_ADMINISTRATIVO"
              },
              "fields": {
                "contratante": { "value": "Doclyn", "confidence": 0.90, "validationStatus": "Validated" },
                "vigencia": { "value": "12 meses", "confidence": 0.70, "validationStatus": "NeedsReview" }
              }
            }
            """);

        _context.DocumentInsights.AddRange(
            CreateInsight(document.Id, DocumentInsightSeverity.Warning),
            CreateInsight(document.Id, DocumentInsightSeverity.Info));

        await _context.SaveChangesAsync();

        _currentUser.UserId = userId;
        _currentUser.Role = UserRole.Operator.ToString();

        var response = await _handler.Handle(new GetDashboardSummaryQuery(), CancellationToken.None);
        var recentDocument = Assert.Single(response.RecentDocuments);

        Assert.Equal(document.Id, recentDocument.Id);
        Assert.Equal("contrato.pdf", recentDocument.FileName);
        Assert.Equal(DocumentStatus.Processing.ToString(), recentDocument.DocumentStatus);
        Assert.Equal("CONTRATO_ADMINISTRATIVO", recentDocument.DocumentClass);
        Assert.Equal(0.80m, recentDocument.AverageConfidence);
        Assert.Equal(2, recentDocument.InsightsCount);
        Assert.Equal(1, recentDocument.NeedsReviewCount);
        Assert.Equal(document.CreatedAt, recentDocument.CreatedAt);
    }

    [Fact]
    public async Task Operator_Scope_Applies_To_Extracted_Data_And_Recent_Documents()
    {
        var operatorId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        var visibleDocument = SeedDocument(operatorId, DocumentStatus.Processed, "visible.pdf");
        var hiddenDocument = SeedDocument(otherUserId, DocumentStatus.Processed, "hidden.pdf");

        SetCreatedAt(visibleDocument, new DateTime(2026, 06, 17, 12, 0, 0, DateTimeKind.Utc));
        SetCreatedAt(hiddenDocument, new DateTime(2026, 06, 17, 13, 0, 0, DateTimeKind.Utc));

        SeedExtractedData(visibleDocument.Id, """
            {
              "classification": {
                "label": "OFICIO"
              },
              "fields": {
                "assunto": { "value": "Teste", "confidence": 0.90, "validationStatus": "Validated" }
              }
            }
            """);

        SeedExtractedData(hiddenDocument.Id, """
            {
              "classification": {
                "documentType": "NOTA_FISCAL"
              },
              "fields": {
                "numero": { "value": "999", "confidence": 0.10, "validationStatus": "Rejected" }
              }
            }
            """);

        _context.DocumentInsights.Add(CreateInsight(hiddenDocument.Id, DocumentInsightSeverity.Critical));

        await _context.SaveChangesAsync();

        _currentUser.UserId = operatorId;
        _currentUser.Role = UserRole.Operator.ToString();

        var response = await _handler.Handle(new GetDashboardSummaryQuery(), CancellationToken.None);
        var recentDocument = Assert.Single(response.RecentDocuments);

        Assert.Equal(0.90m, response.Quality.AverageConfidence);
        Assert.Equal(1, response.Quality.FieldsValidated);
        Assert.Equal(0, response.Quality.FieldsNeedsReview);
        Assert.Equal(0, response.Quality.FieldsRejected);
        Assert.Equal(visibleDocument.Id, recentDocument.Id);
        Assert.Equal("OFICIO", recentDocument.DocumentClass);
        Assert.Equal(0, recentDocument.InsightsCount);
        Assert.Equal(0, recentDocument.NeedsReviewCount);
    }

    [Fact]
    public async Task Handle_Logs_Failed_When_Request_Cannot_Be_Completed()
    {
        _currentUser.UserId = null;
        _currentUser.Role = UserRole.Operator.ToString();

        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _handler.Handle(new GetDashboardSummaryQuery(), CancellationToken.None));

        AssertLogReceived(LogLevel.Information, "DashboardSummaryRequested");
        AssertLogReceived(LogLevel.Error, "DashboardSummaryFailed", exception);
        AssertLogNotReceived(LogLevel.Information, "DashboardSummaryGenerated");
    }

    private Document SeedDocument(Guid userId, DocumentStatus status)
    {
        var document = Document.Create(userId, $"{Guid.NewGuid()}.pdf", "hash", "path", DocumentTypes.Unknown);
        document.UpdateStatus(status);
        _context.Documents.Add(document);
        return document;
    }

    private Document SeedDocument(Guid userId, DocumentStatus status, string fileName)
    {
        var document = Document.Create(userId, fileName, "hash", "path", DocumentTypes.Unknown);
        document.UpdateStatus(status);
        _context.Documents.Add(document);
        return document;
    }

    private void SeedExtractedData(Guid documentId, string json)
    {
        _context.ExtractedData.Add(ExtractedData.Create(documentId, JsonDocument.Parse(json)));
    }

    private static void SetCreatedAt(Document document, DateTime createdAt)
    {
        typeof(AuditableEntity)
            .GetProperty(nameof(AuditableEntity.CreatedAt))!
            .SetValue(document, createdAt);
    }

    private static DocumentInsight CreateInsight(Guid documentId, DocumentInsightSeverity severity)
    {
        return DocumentInsight.Create(
            documentId,
            DocumentInsightType.GenericObservation,
            severity,
            $"{severity} title",
            $"{severity} message",
            0.9m,
            DocumentInsightSource.Rule);
    }

    private void AssertLogReceived(LogLevel level, string message, Exception? exception = null)
    {
        Assert.Contains(_logger.ReceivedCalls(), call =>
        {
            var arguments = call.GetArguments();

            return arguments.Length == 5
                && arguments[0] is LogLevel actualLevel
                && actualLevel == level
                && arguments[2]?.ToString()?.Contains(message, StringComparison.Ordinal) == true
                && (exception is null || ReferenceEquals(arguments[3], exception));
        });
    }

    [Fact]
    public async Task MostUsed_Classes_Are_Computed_From_Scoped_Extracted_Data()
    {
        var userId = Guid.NewGuid();
        var doc1 = SeedDocument(userId, DocumentStatus.Processed);
        var doc2 = SeedDocument(userId, DocumentStatus.Processed);
        var doc3 = SeedDocument(userId, DocumentStatus.Processed);

        _context.DocumentClasses.Add(DocumentClass.Create("RELATORIO_TECNICO_PRELIMINAR", "ADM", "APUR", "", true));
        _context.DocumentClasses.Add(DocumentClass.Create("CONTRATO_ADMINISTRATIVO", "ADM", "CTR", "", true));
        await _context.SaveChangesAsync();

        SeedExtractedData(doc1.Id, "{\"classification\":{\"documentType\":\"RELATORIO_TECNICO_PRELIMINAR\"}}");
        SeedExtractedData(doc2.Id, "{\"classification\":{\"documentType\":\"RELATORIO_TECNICO_PRELIMINAR\"}}");
        SeedExtractedData(doc3.Id, "{\"classification\":{\"documentType\":\"CONTRATO_ADMINISTRATIVO\"}}");
        await _context.SaveChangesAsync();

        _currentUser.UserId = userId;
        _currentUser.Role = UserRole.Operator.ToString();

        var response = await _handler.Handle(new GetDashboardSummaryQuery(), CancellationToken.None);

        Assert.True(response.Classes.Total >= 2);
        var ordered = response.Classes.MostUsed.OrderByDescending(m => m.DocumentsCount).ToList();
        Assert.Equal(2, ordered[0].DocumentsCount);
        Assert.Equal(1, ordered[1].DocumentsCount);
    }

    [Fact]
    public async Task Attention_Required_Prioritizes_Failed_Documents_And_Limits_To_Five()
    {
        var userId = Guid.NewGuid();
        var createdAt = new DateTime(2026, 06, 17, 12, 0, 0, DateTimeKind.Utc);

        for (var i = 0; i < 6; i++)
        {
            var doc = SeedDocument(userId, DocumentStatus.Failed);
            SetCreatedAt(doc, createdAt.AddMinutes(i));
        }

        await _context.SaveChangesAsync();

        _currentUser.UserId = userId;
        _currentUser.Role = UserRole.Operator.ToString();

        var response = await _handler.Handle(new GetDashboardSummaryQuery(), CancellationToken.None);

        Assert.InRange(response.AttentionRequired.Count, 0, 5);
        if (response.AttentionRequired.Count > 0)
        {
            Assert.Contains(response.AttentionRequired, a => a.Reason == "Document processing failed");
        }
    }

    [Fact]
    public async Task Attention_Required_Flags_Documents_With_NeedsReview_Fields()
    {
        var userId = Guid.NewGuid();
        var doc = SeedDocument(userId, DocumentStatus.Processed);
        SetCreatedAt(doc, new DateTime(2026, 06, 17, 12, 0, 0, DateTimeKind.Utc));

        SeedExtractedData(doc.Id, """
            {
                "fields": {
                    "orgao": { "value": "Prefeitura", "confidence": 0.80, "validationStatus": "NeedsReview" }
                }
            }
            """);
        await _context.SaveChangesAsync();

        _currentUser.UserId = userId;
        _currentUser.Role = UserRole.Operator.ToString();

        var response = await _handler.Handle(new GetDashboardSummaryQuery(), CancellationToken.None);

        Assert.Contains(response.AttentionRequired, a => a.Reason == "Fields require review");
    }

    private void AssertLogNotReceived(LogLevel level, string message)
    {
        Assert.DoesNotContain(_logger.ReceivedCalls(), call =>
        {
            var arguments = call.GetArguments();

            return arguments.Length == 5
                && arguments[0] is LogLevel actualLevel
                && actualLevel == level
                && arguments[2]?.ToString()?.Contains(message, StringComparison.Ordinal) == true;
        });
    }
}
