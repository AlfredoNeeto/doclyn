using Doclyn.Application.Common.Interfaces;
using Doclyn.Application.DocumentExtraction.ClassGuidedExtraction;
using Doclyn.Application.Documents.Processing;
using Doclyn.Domain.Entities;
using Doclyn.Domain.Enums;
using Doclyn.Infrastructure.Extraction;
using Doclyn.Infrastructure.Validation;
using Doclyn.UnitTests.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Doclyn.UnitTests.Extraction;

public sealed class ClassGuidedExtractionServiceTests : IDisposable
{
    private readonly TestDbContext _context;
    private readonly IDocumentClassIndexerCatalogService _documentClassIndexerCatalogService;
    private readonly IDocumentIndexer _documentIndexer;
    private readonly IAiStructuredDataExtractor _aiStructuredDataExtractor;
    private readonly IFieldValidationService _fieldValidationService;
    private readonly ClassGuidedExtractionService _service;
    private readonly List<DocumentClassIndexer> _activeIndexers;
    private readonly DocumentClass _documentClass;

    public ClassGuidedExtractionServiceTests()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new TestDbContext(options);
        _documentClassIndexerCatalogService = Substitute.For<IDocumentClassIndexerCatalogService>();
        _documentIndexer = Substitute.For<IDocumentIndexer>();
        _aiStructuredDataExtractor = Substitute.For<IAiStructuredDataExtractor>();
        _fieldValidationService = Substitute.For<IFieldValidationService>();
        var logger = Substitute.For<ILogger<ClassGuidedExtractionService>>();
        var confidenceOptions = Options.Create(new FieldConfidenceOptions
        {
            ValidatedThreshold = 0.90m,
            ReviewThreshold = 0.70m,
            DefaultAiConfidence = 0.80m
        });

        _fieldValidationService.DetermineStatus(Arg.Any<decimal>())
            .Returns(call =>
            {
                var confidence = call.ArgAt<decimal>(0);
                return confidence >= 0.90m ? ValidationStatus.Validated
                    : confidence >= 0.70m ? ValidationStatus.NeedsReview
                    : ValidationStatus.Rejected;
            });

        _documentClass = DocumentClass.Create(
            "RELATORIO_TECNICO_PRELIMINAR",
            "ADMINISTRATIVO",
            "PROCESSO_ADMINISTRATIVO",
            "Descricao da classe.",
            isSystemDefined: true);
        _context.DocumentClasses.Add(_documentClass);
        _context.SaveChanges();

        _activeIndexers =
        [
            DocumentClassIndexer.Create(
                _documentClass.Id,
                "numeroProcesso",
                "Numero do Processo",
                "Desc.",
                IndexerDataType.Text,
                isRequired: true,
                isMultiple: false,
                regexPattern: @"PROCESSO\s+N[º°]?\s*([\d\/.-]+)"),
            DocumentClassIndexer.Create(
                _documentClass.Id,
                "orgao",
                "Orgao",
                "Nome do orgao.",
                IndexerDataType.Text,
                isRequired: false,
                isMultiple: false,
                extractionHint: "Buscar por prefeitura.")
        ];

        _documentClassIndexerCatalogService
            .GetActiveByDocumentClassAsync(
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>())
            .Returns(_activeIndexers);

        _documentIndexer
            .ExtractIndexes(Arg.Any<string>(), Arg.Any<IReadOnlyCollection<DocumentClassIndexer>>())
            .Returns(new Dictionary<string, DocumentIndexerValue>
            {
                ["numeroProcesso"] = new("2026/12345", "Regex", 1.0)
            });

        _aiStructuredDataExtractor
            .ExtractAsync(
                Arg.Any<string>(),
                Arg.Any<DocumentClass>(),
                Arg.Any<IReadOnlyCollection<DocumentClassIndexer>>(),
                Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, object?> { ["orgao"] = "Prefeitura Municipal de Vale Verde" });

        _service = new ClassGuidedExtractionService(
            _context,
            _documentClassIndexerCatalogService,
            _documentIndexer,
            _aiStructuredDataExtractor,
            _fieldValidationService,
            confidenceOptions,
            logger);
    }

    [Fact]
    public async Task Should_Load_Active_Indexers_For_Class()
    {
        var result = await _service.ExtractAsync(_documentClass.Id, "some text");

        Assert.Equal(_documentClass.Id, result.DocumentClassId);
        Assert.NotNull(result.Fields);
    }

    [Fact]
    public async Task Should_Return_Regex_Fields_With_Validated_Status()
    {
        var result = await _service.ExtractAsync(_documentClass.Id, "PROCESSO Nº 2026/12345");

        Assert.True(result.Fields.ContainsKey("numeroProcesso"));
        Assert.Equal("2026/12345", result.Fields["numeroProcesso"].Value);
        Assert.Equal(ExtractionSource.Regex, result.Fields["numeroProcesso"].Source);
        Assert.Equal(1.0m, result.Fields["numeroProcesso"].Confidence);
        Assert.Equal(ValidationStatus.Validated, result.Fields["numeroProcesso"].ValidationStatus);
    }

    [Fact]
    public async Task Should_Call_AI_Only_For_Missing_Fields()
    {
        var result = await _service.ExtractAsync(_documentClass.Id, "PROCESSO Nº 2026/12345");

        Assert.True(result.Fields.ContainsKey("orgao"));
        Assert.Equal(ExtractionSource.AI, result.Fields["orgao"].Source);
        Assert.Equal("Prefeitura Municipal de Vale Verde", result.Fields["orgao"].Value);
    }

    [Fact]
    public async Task Should_Not_Call_AI_When_Regex_Covers_All_Fields()
    {
        _documentIndexer
            .ExtractIndexes(Arg.Any<string>(), Arg.Any<IReadOnlyCollection<DocumentClassIndexer>>())
            .Returns(new Dictionary<string, DocumentIndexerValue>
            {
                ["numeroProcesso"] = new("2026/98765", "Regex", 1.0),
                ["orgao"] = new("Prefeitura X", "Regex", 1.0)
            });

        _aiStructuredDataExtractor.ClearReceivedCalls();

        await _service.ExtractAsync(_documentClass.Id, "some text");

        await _aiStructuredDataExtractor
            .DidNotReceive()
            .ExtractAsync(
                Arg.Any<string>(),
                Arg.Any<DocumentClass>(),
                Arg.Any<IReadOnlyCollection<DocumentClassIndexer>>(),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_Prioritize_Regex_Over_AI()
    {
        _documentIndexer
            .ExtractIndexes(Arg.Any<string>(), Arg.Any<IReadOnlyCollection<DocumentClassIndexer>>())
            .Returns(new Dictionary<string, DocumentIndexerValue>
            {
                ["numeroProcesso"] = new("REGEX_VALUE", "Regex", 1.0),
                ["orgao"] = new("REGEX_ORGAO", "Regex", 1.0)
            });

        var result = await _service.ExtractAsync(_documentClass.Id, "some text");

        Assert.Equal("REGEX_VALUE", result.Fields["numeroProcesso"].Value);
        Assert.Equal(ExtractionSource.Regex, result.Fields["numeroProcesso"].Source);
        Assert.Equal("REGEX_ORGAO", result.Fields["orgao"].Value);
        Assert.Equal(ExtractionSource.Regex, result.Fields["orgao"].Source);
    }

    [Fact]
    public async Task Should_Return_Empty_Fields_When_No_Active_Indexers()
    {
        _documentClassIndexerCatalogService
            .GetActiveByDocumentClassAsync(
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>())
            .Returns(Array.Empty<DocumentClassIndexer>());

        var result = await _service.ExtractAsync(_documentClass.Id, "some text");

        Assert.NotNull(result.Fields);
        Assert.Empty(result.Fields);
    }

    [Fact]
    public async Task Should_Work_With_New_Class_Without_Code_Changes()
    {
        var newClass = DocumentClass.Create(
            "PARECER_JURIDICO",
            "JURIDICO",
            "CONSULTIVO",
            "Parecer juridico.",
            isSystemDefined: false);
        _context.DocumentClasses.Add(newClass);
        await _context.SaveChangesAsync();

        var newIndexers = new List<DocumentClassIndexer>
        {
            DocumentClassIndexer.Create(
                newClass.Id,
                "parecerista",
                "Parecerista",
                "Nome do parecerista.",
                IndexerDataType.Text,
                isRequired: false,
                isMultiple: false,
                extractionHint: "Buscar por nome apos 'Parecerista:'.")
        };

        _documentClassIndexerCatalogService
            .GetActiveByDocumentClassAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(newIndexers);

        _documentIndexer
            .ExtractIndexes(Arg.Any<string>(), Arg.Any<IReadOnlyCollection<DocumentClassIndexer>>())
            .Returns(new Dictionary<string, DocumentIndexerValue>());

        _aiStructuredDataExtractor
            .ExtractAsync(
                Arg.Any<string>(),
                Arg.Any<DocumentClass>(),
                Arg.Any<IReadOnlyCollection<DocumentClassIndexer>>(),
                Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, object?> { ["parecerista"] = "Dr. Silva" });

        var result = await _service.ExtractAsync(newClass.Id, "Parecerista: Dr. Silva");

        Assert.True(result.Fields.ContainsKey("parecerista"));
        Assert.Equal("Dr. Silva", result.Fields["parecerista"].Value);
        Assert.Equal(ExtractionSource.AI, result.Fields["parecerista"].Source);
    }

    [Fact]
    public async Task Should_Mark_AI_Fields_As_NeedsReview()
    {
        var result = await _service.ExtractAsync(_documentClass.Id, "PROCESSO Nº 2026/12345");

        Assert.Equal(ValidationStatus.NeedsReview, result.Fields["orgao"].ValidationStatus);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
