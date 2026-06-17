using System.Text.Json;
using Doclyn.Application.Common.Interfaces;
using Doclyn.Application.DocumentExtraction.ClassGuidedExtraction;
using Doclyn.Application.Documents.Insights;
using Doclyn.Application.Documents.Processing;
using Doclyn.Domain.Constants;
using Doclyn.Domain.Entities;
using Doclyn.Domain.Enums;
using Doclyn.Infrastructure.AI;
using Doclyn.Infrastructure.OCR;
using Doclyn.Infrastructure.Processing;
using Doclyn.UnitTests.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Doclyn.UnitTests.Documents.Processing;

public sealed class DocumentProcessingServiceTests : IDisposable
{
    private readonly TestDbContext _context;
    private readonly IFileStorageService _fileStorageService;
    private readonly IPdfTextExtractor _pdfTextExtractor;
    private readonly IOcrService _ocrService;
    private readonly IDocumentClassifier _documentClassifier;
    private readonly IDocumentIndexer _documentIndexer;
    private readonly IAiDocumentClassifier _aiDocumentClassifier;
    private readonly IAiStructuredDataExtractor _aiStructuredDataExtractor;
    private readonly IDocumentClassCatalogService _documentClassCatalogService;
    private readonly IDocumentClassIndexerCatalogService _documentClassIndexerCatalogService;
    private readonly IDocumentSemanticClassificationService _documentSemanticClassificationService;
    private readonly IClassGuidedExtractionService _classGuidedExtractionService;
    private readonly IDocumentInsightService _documentInsightService;
    private readonly DocumentProcessingService _service;
    private readonly IReadOnlyCollection<DocumentClassIndexer> _activeIndexers;

    public DocumentProcessingServiceTests()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new TestDbContext(options);
        _fileStorageService = Substitute.For<IFileStorageService>();
        _pdfTextExtractor = Substitute.For<IPdfTextExtractor>();
        _ocrService = Substitute.For<IOcrService>();
        _documentClassifier = Substitute.For<IDocumentClassifier>();
        _documentIndexer = Substitute.For<IDocumentIndexer>();
        _aiDocumentClassifier = Substitute.For<IAiDocumentClassifier>();
        _aiStructuredDataExtractor = Substitute.For<IAiStructuredDataExtractor>();
        _documentClassCatalogService = Substitute.For<IDocumentClassCatalogService>();
        _documentClassIndexerCatalogService = Substitute.For<IDocumentClassIndexerCatalogService>();
        _documentSemanticClassificationService = Substitute.For<IDocumentSemanticClassificationService>();
        _classGuidedExtractionService = Substitute.For<IClassGuidedExtractionService>();
        _documentInsightService = Substitute.For<IDocumentInsightService>();
        var logger = Substitute.For<ILogger<DocumentProcessingService>>();
        var extractionMergeService = new ExtractionMergeService();
        var documentTextMergeService = new DocumentTextMergeService();
        var ocrOptions = Options.Create(new Doclyn.Infrastructure.OCR.OcrOptions
        {
            Enabled = true,
            MinimumTextLength = 100
        });
        var ocrProcessingContextAccessor = new Doclyn.Infrastructure.OCR.OcrProcessingContextAccessor();
        var documentClassId = Guid.NewGuid();

        _activeIndexers =
        [
            DocumentClassIndexer.Create(
                documentClassId,
                "numeroProcesso",
                "Numero do Processo",
                string.Empty,
                IndexerDataType.Text,
                isRequired: true,
                isMultiple: false,
                regexPattern: @"PROCESSO\s+ADMINISTRATIVO\s+N[º°]?\s*([\d\/.-]+)")
        ];

        _documentSemanticClassificationService
            .ClassifyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call => new SemanticClassificationResult(
                DocumentClassId: documentClassId,
                DocumentType: DocumentTypes.RelatorioTecnicoPreliminar,
                Group: "ADMINISTRATIVO",
                SubGroup: "PROCESSO_ADMINISTRATIVO",
                Confidence: 0.95m,
                ReusedExistingClass: true,
                NewClassSuggested: false));

        _documentClassCatalogService
            .GetOrCreateAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var name = call.ArgAt<string>(0);
                var group = call.ArgAt<string>(1);
                var subGroup = call.ArgAt<string>(2);
                var description = call.ArgAt<string>(3);
                return DocumentClass.Create(name, group, subGroup, description);
            });

        _documentClassCatalogService
            .RegisterExampleAsync(
                Arg.Any<Guid>(),
                Arg.Any<Guid>(),
                Arg.Any<decimal>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        _documentClassCatalogService
            .FindByNameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call => DocumentClass.Create(
                call.ArgAt<string>(0),
                "ADMINISTRATIVO",
                "PROCESSO_ADMINISTRATIVO",
                "Test class"));

        _documentClassIndexerCatalogService
            .GetActiveByDocumentClassAsync(
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>())
            .Returns(_activeIndexers);

        _classGuidedExtractionService
            .ExtractAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call => new ClassGuidedExtractionResult(call.ArgAt<Guid>(0), []));

        _documentInsightService
            .GenerateAsync(Arg.Any<Guid>(), Arg.Any<ExtractedDocumentData>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<DocumentInsightResult>());

        _ocrService.ExtractTextAsync(Arg.Any<Stream>(), Arg.Any<CancellationToken>())
            .Returns("""
                RELATÓRIO TÉCNICO PRELIMINAR
                PROCESSO ADMINISTRATIVO Nº 2026/98765
                CONTRATO nº 45/2026
                PREFEITURA MUNICIPAL DE VALE VERDE
                CNPJ 12.345.678/0001-99
                """);

        _service = new DocumentProcessingService(
            _context,
            _context,
            _fileStorageService,
            _pdfTextExtractor,
            _ocrService,
            _documentClassifier,
            _documentIndexer,
            _aiDocumentClassifier,
            _aiStructuredDataExtractor,
            _documentClassCatalogService,
            _documentClassIndexerCatalogService,
            _documentSemanticClassificationService,
            _classGuidedExtractionService,
            _documentInsightService,
            extractionMergeService,
            ocrOptions,
            ocrProcessingContextAccessor,
            documentTextMergeService,
            logger);
    }

    [Fact]
    public async Task Should_Process_Document_And_Save_Extracted_Data()
    {
        var document = Document.Create(Guid.NewGuid(), "file.pdf", "hash", "documents/test/original.pdf", DocumentTypes.Unknown);
        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        _fileStorageService.DownloadAsync(document.StoragePath, Arg.Any<CancellationToken>())
            .Returns(new MemoryStream([1, 2, 3]));
        _pdfTextExtractor.ExtractTextAsync(Arg.Any<Stream>(), Arg.Any<CancellationToken>())
            .Returns("""
                RELATÓRIO TÉCNICO PRELIMINAR
                PROCESSO ADMINISTRATIVO Nº 2026/98765
                CONTRATO nº 45/2026
                PREFEITURA MUNICIPAL DE VALE VERDE
                CNPJ 12.345.678/0001-99
                """);
        _documentClassifier.Classify(Arg.Any<string>())
            .Returns(new DocumentClassificationResult(DocumentTypes.RelatorioTecnicoPreliminar, "PROCESSO_ADMINISTRATIVO", "APURACAO_CONTRATUAL", 1.0));
        _aiDocumentClassifier.ClassifyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new DocumentClassificationResult(DocumentTypes.RelatorioTecnicoPreliminar, "PROCESSO_ADMINISTRATIVO", "APURACAO_CONTRATUAL", 0.98));
        _documentIndexer.ExtractIndexes(Arg.Any<string>(), Arg.Any<IReadOnlyCollection<DocumentClassIndexer>>())
            .Returns(new Dictionary<string, DocumentIndexerValue>
            {
                ["numeroProcesso"] = new("2026/98765", "Regex", 1.0)
            });
        _aiStructuredDataExtractor.ExtractAsync(
                Arg.Any<string>(),
                Arg.Any<DocumentClass>(),
                Arg.Any<IReadOnlyCollection<DocumentClassIndexer>>(),
                Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, object?>
            {
                ["numeroProcesso"] = "2026/98765-ai",
                ["orgao"] = "Prefeitura Municipal de Vale Verde"
            });
        _classGuidedExtractionService.ExtractAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var classId = call.ArgAt<Guid>(0);
                var fields = new Dictionary<string, ExtractedFieldResult>(StringComparer.OrdinalIgnoreCase)
                {
                    ["numeroProcesso"] = new("2026/98765", 1.0m, ExtractionSource.Regex, ValidationStatus.Validated)
                };
                return new ClassGuidedExtractionResult(classId, fields);
            });

        await _service.ProcessAsync(document.Id, CancellationToken.None);

        var persistedDocument = await _context.Documents.SingleAsync(d => d.Id == document.Id);
        var extractedData = await _context.ExtractedData.SingleAsync(ed => ed.DocumentId == document.Id);
        var logs = await _context.ProcessingLogs.Where(log => log.DocumentId == document.Id).ToListAsync();

        Assert.Equal(DocumentStatus.Processed, persistedDocument.DocumentStatus);
        Assert.Equal(DocumentTypes.RelatorioTecnicoPreliminar, persistedDocument.DocumentType);
        Assert.Equal("2026/98765", extractedData.Data.RootElement.GetProperty("finalResult").GetProperty("numeroProcesso").GetString());
        Assert.Equal(0.95, extractedData.Data.RootElement.GetProperty("classification").GetProperty("confidence").GetDouble());
        Assert.Equal("2026/98765", extractedData.Data.RootElement.GetProperty("indexers").GetProperty("numeroProcesso").GetProperty("value").GetString());
        Assert.Contains(logs, log => log.Step == "ProcessingStarted");
        Assert.Contains(logs, log => log.Step == "ProcessingCompleted");
        Assert.Contains(logs, log => log.Step == "OcrStarted");
        Assert.Contains(logs, log => log.Step == "OcrCompleted");
        Assert.Contains(logs, log => log.Step == "ExistingClassMatched");
        Assert.Contains(logs, log => log.Step == "AiExtractionCompleted");
        Assert.Contains(logs, log => log.Step == "DocumentClassReused");
        Assert.Contains(logs, log => log.Step == "DocumentClassIndexersLoaded");
    }

    [Fact]
    public async Task Should_Fail_When_Extracted_Text_Is_Insufficient()
    {
        var document = Document.Create(Guid.NewGuid(), "file.pdf", "hash", "documents/test/original.pdf", DocumentTypes.Unknown);
        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        _fileStorageService.DownloadAsync(document.StoragePath, Arg.Any<CancellationToken>())
            .Returns(new MemoryStream([1, 2, 3]));
        _pdfTextExtractor.ExtractTextAsync(Arg.Any<Stream>(), Arg.Any<CancellationToken>())
            .Returns("short text");
        _ocrService.ExtractTextAsync(Arg.Any<Stream>(), Arg.Any<CancellationToken>())
            .Returns(string.Empty);

        await _service.ProcessAsync(document.Id, CancellationToken.None);

        var persistedDocument = await _context.Documents.SingleAsync(d => d.Id == document.Id);
        var failureLog = await _context.ProcessingLogs.SingleAsync(log => log.DocumentId == document.Id && log.Step == "ProcessingFailed");

        Assert.Equal(DocumentStatus.Failed, persistedDocument.DocumentStatus);
        Assert.Contains("insufficient", failureLog.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Should_Use_Ocr_When_Initial_Text_Is_Insufficient()
    {
        var document = Document.Create(Guid.NewGuid(), "file.pdf", "hash", "documents/test/original.pdf", DocumentTypes.Unknown);
        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        _fileStorageService.DownloadAsync(document.StoragePath, Arg.Any<CancellationToken>())
            .Returns(new MemoryStream([1, 2, 3]));
        _pdfTextExtractor.ExtractTextAsync(Arg.Any<Stream>(), Arg.Any<CancellationToken>())
            .Returns("short text");
        _ocrService.ExtractTextAsync(Arg.Any<Stream>(), Arg.Any<CancellationToken>())
            .Returns("""
                RELATÓRIO TÉCNICO PRELIMINAR
                PROCESSO ADMINISTRATIVO Nº 2026/98765
                CONTRATO nº 45/2026
                PREFEITURA MUNICIPAL DE VALE VERDE
                CNPJ 12.345.678/0001-99
                """);
        _documentClassifier.Classify(Arg.Any<string>())
            .Returns(new DocumentClassificationResult(DocumentTypes.RelatorioTecnicoPreliminar, "PROCESSO_ADMINISTRATIVO", "APURACAO_CONTRATUAL", 1.0));
        _documentIndexer.ExtractIndexes(Arg.Any<string>(), Arg.Any<IReadOnlyCollection<DocumentClassIndexer>>())
            .Returns(new Dictionary<string, DocumentIndexerValue>());
        _aiDocumentClassifier.ClassifyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new DocumentClassificationResult(DocumentTypes.RelatorioTecnicoPreliminar, "PROCESSO_ADMINISTRATIVO", "APURACAO_CONTRATUAL", 0.93));
        _aiStructuredDataExtractor.ExtractAsync(
                Arg.Any<string>(),
                Arg.Any<DocumentClass>(),
                Arg.Any<IReadOnlyCollection<DocumentClassIndexer>>(),
                Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, object?>());

        await _service.ProcessAsync(document.Id, CancellationToken.None);

        var persistedDocument = await _context.Documents.SingleAsync(d => d.Id == document.Id);
        var logs = await _context.ProcessingLogs.Where(log => log.DocumentId == document.Id).ToListAsync();

        Assert.Equal(DocumentStatus.Failed, persistedDocument.DocumentStatus);
        Assert.Contains(logs, log => log.Step == "OcrStarted");
        Assert.Contains(logs, log => log.Step == "OcrCompleted");
        Assert.Contains(logs, log => log.Step == "ExtractionInsufficient");
    }

    [Fact]
    public async Task Should_Fail_When_No_Useful_Field_Is_Extracted()
    {
        var document = Document.Create(Guid.NewGuid(), "file.pdf", "hash", "documents/test/original.pdf", DocumentTypes.Unknown);
        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        _fileStorageService.DownloadAsync(document.StoragePath, Arg.Any<CancellationToken>())
            .Returns(new MemoryStream([1, 2, 3]));
        _pdfTextExtractor.ExtractTextAsync(Arg.Any<Stream>(), Arg.Any<CancellationToken>())
            .Returns("RELATÓRIO TÉCNICO PRELIMINAR PROCESSO ADMINISTRATIVO Nº 2026/98765 CONTRATO nº 45/2026 PREFEITURA MUNICIPAL DE VALE VERDE CNPJ 12.345.678/0001-99");
        _documentClassifier.Classify(Arg.Any<string>())
            .Returns(new DocumentClassificationResult(DocumentTypes.RelatorioTecnicoPreliminar, "PROCESSO_ADMINISTRATIVO", "APURACAO_CONTRATUAL", 1.0));
        _documentIndexer.ExtractIndexes(Arg.Any<string>(), Arg.Any<IReadOnlyCollection<DocumentClassIndexer>>())
            .Returns(new Dictionary<string, DocumentIndexerValue>());
        _aiDocumentClassifier.ClassifyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new DocumentClassificationResult(DocumentTypes.RelatorioTecnicoPreliminar, "PROCESSO_ADMINISTRATIVO", "APURACAO_CONTRATUAL", 0.98));
        _aiStructuredDataExtractor.ExtractAsync(
                Arg.Any<string>(),
                Arg.Any<DocumentClass>(),
                Arg.Any<IReadOnlyCollection<DocumentClassIndexer>>(),
                Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, object?>());
        _classGuidedExtractionService.ExtractAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call => new ClassGuidedExtractionResult(call.ArgAt<Guid>(0), []));

        await _service.ProcessAsync(document.Id, CancellationToken.None);

        var persistedDocument = await _context.Documents.SingleAsync(d => d.Id == document.Id);
        var extractionLog = await _context.ProcessingLogs.SingleAsync(log => log.DocumentId == document.Id && log.Step == "ExtractionInsufficient");

        Assert.Equal(DocumentStatus.Failed, persistedDocument.DocumentStatus);
        Assert.Contains("No useful fields", extractionLog.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Should_Process_When_At_Least_One_Useful_Field_Is_Extracted()
    {
        var document = Document.Create(Guid.NewGuid(), "file.pdf", "hash", "documents/test/original.pdf", DocumentTypes.Unknown);
        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        _fileStorageService.DownloadAsync(document.StoragePath, Arg.Any<CancellationToken>())
            .Returns(new MemoryStream([1, 2, 3]));
        _pdfTextExtractor.ExtractTextAsync(Arg.Any<Stream>(), Arg.Any<CancellationToken>())
            .Returns("RELATÓRIO TÉCNICO PRELIMINAR PROCESSO ADMINISTRATIVO Nº 2026/98765 CONTRATO nº 45/2026 PREFEITURA MUNICIPAL DE VALE VERDE CNPJ 12.345.678/0001-99");
        _documentClassifier.Classify(Arg.Any<string>())
            .Returns(new DocumentClassificationResult(DocumentTypes.RelatorioTecnicoPreliminar, "PROCESSO_ADMINISTRATIVO", "APURACAO_CONTRATUAL", 1.0));
        _documentIndexer.ExtractIndexes(Arg.Any<string>(), Arg.Any<IReadOnlyCollection<DocumentClassIndexer>>())
            .Returns(new Dictionary<string, DocumentIndexerValue>
            {
                ["numeroProcesso"] = new("2026/98765", "Regex", 1.0)
            });
        _aiDocumentClassifier.ClassifyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new DocumentClassificationResult(DocumentTypes.RelatorioTecnicoPreliminar, "PROCESSO_ADMINISTRATIVO", "APURACAO_CONTRATUAL", 0.98));
        _aiStructuredDataExtractor.ExtractAsync(
                Arg.Any<string>(),
                Arg.Any<DocumentClass>(),
                Arg.Any<IReadOnlyCollection<DocumentClassIndexer>>(),
                Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, object?>());
        _classGuidedExtractionService.ExtractAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var classId = call.ArgAt<Guid>(0);
                var fields = new Dictionary<string, ExtractedFieldResult>(StringComparer.OrdinalIgnoreCase)
                {
                    ["numeroProcesso"] = new("2026/98765", 1.0m, ExtractionSource.Regex, ValidationStatus.Validated)
                };
                return new ClassGuidedExtractionResult(classId, fields);
            });

        await _service.ProcessAsync(document.Id, CancellationToken.None);

        var persistedDocument = await _context.Documents.SingleAsync(d => d.Id == document.Id);

        Assert.Equal(DocumentStatus.Processed, persistedDocument.DocumentStatus);
    }

    [Fact]
    public async Task Should_Log_AiExtractionEmpty_When_Ai_Returns_No_Fields()
    {
        var document = Document.Create(Guid.NewGuid(), "file.pdf", "hash", "documents/test/original.pdf", DocumentTypes.Unknown);
        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        _fileStorageService.DownloadAsync(document.StoragePath, Arg.Any<CancellationToken>())
            .Returns(new MemoryStream([1, 2, 3]));
        _pdfTextExtractor.ExtractTextAsync(Arg.Any<Stream>(), Arg.Any<CancellationToken>())
            .Returns("RELATÓRIO TÉCNICO PRELIMINAR PROCESSO ADMINISTRATIVO Nº 2026/98765 CONTRATO nº 45/2026 PREFEITURA MUNICIPAL DE VALE VERDE CNPJ 12.345.678/0001-99");
        _documentClassifier.Classify(Arg.Any<string>())
            .Returns(new DocumentClassificationResult(DocumentTypes.RelatorioTecnicoPreliminar, "PROCESSO_ADMINISTRATIVO", "APURACAO_CONTRATUAL", 1.0));
        _documentIndexer.ExtractIndexes(Arg.Any<string>(), Arg.Any<IReadOnlyCollection<DocumentClassIndexer>>())
            .Returns(new Dictionary<string, DocumentIndexerValue>
            {
                ["numeroProcesso"] = new("2026/98765", "Regex", 1.0)
            });
        _aiDocumentClassifier.ClassifyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new DocumentClassificationResult(DocumentTypes.RelatorioTecnicoPreliminar, "PROCESSO_ADMINISTRATIVO", "APURACAO_CONTRATUAL", 0.98));
        _aiStructuredDataExtractor.ExtractAsync(
                Arg.Any<string>(),
                Arg.Any<DocumentClass>(),
                Arg.Any<IReadOnlyCollection<DocumentClassIndexer>>(),
                Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, object?>());
        _classGuidedExtractionService.ExtractAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var classId = call.ArgAt<Guid>(0);
                var fields = new Dictionary<string, ExtractedFieldResult>(StringComparer.OrdinalIgnoreCase)
                {
                    ["numeroProcesso"] = new("2026/98765", 1.0m, ExtractionSource.Regex, ValidationStatus.Validated)
                };
                return new ClassGuidedExtractionResult(classId, fields);
            });

        await _service.ProcessAsync(document.Id, CancellationToken.None);

        var logs = await _context.ProcessingLogs.Where(log => log.DocumentId == document.Id).ToListAsync();

        Assert.Contains(logs, log => log.Step == "AiExtractionEmpty");
        Assert.Contains(logs, log => log.Step == "AiExtractionStarted");
    }

    [Fact]
    public async Task Should_Log_ExtractionLowQuality_When_Few_Fields_With_Low_Validation()
    {
        var document = Document.Create(Guid.NewGuid(), "file.pdf", "hash", "documents/test/original.pdf", DocumentTypes.Unknown);
        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        _fileStorageService.DownloadAsync(document.StoragePath, Arg.Any<CancellationToken>())
            .Returns(new MemoryStream([1, 2, 3]));
        _pdfTextExtractor.ExtractTextAsync(Arg.Any<Stream>(), Arg.Any<CancellationToken>())
            .Returns("RELATÓRIO TÉCNICO PRELIMINAR PROCESSO ADMINISTRATIVO Nº 2026/98765 CONTRATO nº 45/2026 PREFEITURA MUNICIPAL DE VALE VERDE CNPJ 12.345.678/0001-99");
        _documentClassifier.Classify(Arg.Any<string>())
            .Returns(new DocumentClassificationResult(DocumentTypes.RelatorioTecnicoPreliminar, "PROCESSO_ADMINISTRATIVO", "APURACAO_CONTRATUAL", 1.0));
        _documentIndexer.ExtractIndexes(Arg.Any<string>(), Arg.Any<IReadOnlyCollection<DocumentClassIndexer>>())
            .Returns(new Dictionary<string, DocumentIndexerValue>());
        _aiDocumentClassifier.ClassifyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new DocumentClassificationResult(DocumentTypes.RelatorioTecnicoPreliminar, "PROCESSO_ADMINISTRATIVO", "APURACAO_CONTRATUAL", 0.98));
        _aiStructuredDataExtractor.ExtractAsync(
                Arg.Any<string>(),
                Arg.Any<DocumentClass>(),
                Arg.Any<IReadOnlyCollection<DocumentClassIndexer>>(),
                Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, object?>());
        _classGuidedExtractionService.ExtractAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var classId = call.ArgAt<Guid>(0);
                var fields = new Dictionary<string, ExtractedFieldResult>(StringComparer.OrdinalIgnoreCase)
                {
                    ["orgao"] = new("Prefeitura", 0.65m, ExtractionSource.AI, ValidationStatus.NeedsReview)
                };
                return new ClassGuidedExtractionResult(classId, fields);
            });

        await _service.ProcessAsync(document.Id, CancellationToken.None);

        var persistedDocument = await _context.Documents.SingleAsync(d => d.Id == document.Id);
        var logs = await _context.ProcessingLogs.Where(log => log.DocumentId == document.Id).ToListAsync();

        Assert.Equal(DocumentStatus.Processed, persistedDocument.DocumentStatus);
        Assert.Contains(logs, log => log.Step == "ExtractionLowQuality");
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
