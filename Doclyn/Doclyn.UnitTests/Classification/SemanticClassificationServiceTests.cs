using Doclyn.Application.Common.Interfaces;
using Doclyn.Application.Documents.Processing;
using Doclyn.Domain.Entities;
using Doclyn.Infrastructure.AI;
using Doclyn.Infrastructure.Classification;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Doclyn.UnitTests.Classification;

public sealed class SemanticClassificationServiceTests
{
    private readonly IDocumentClassCatalogService _documentClassCatalogService;
    private readonly OpenAiSemanticClassifier _semanticClassifier;
    private readonly ClassificationOptions _options;
    private readonly SemanticClassificationService _service;
    private readonly List<DocumentClass> _activeClasses;

    public SemanticClassificationServiceTests()
    {
        _documentClassCatalogService = Substitute.For<IDocumentClassCatalogService>();
        _semanticClassifier = Substitute.For<OpenAiSemanticClassifier>(
            Substitute.For<OpenAiClientFactory>(Options.Create(new OpenAiOptions { Model = "test", ApiKey = "test" })),
            Substitute.For<PromptBuilder>(),
            Options.Create(new OpenAiOptions { Model = "test", ApiKey = "test" }),
            Substitute.For<ILogger<OpenAiSemanticClassifier>>());

        _options = new ClassificationOptions { ReuseThreshold = 0.85m };
        var logger = Substitute.For<ILogger<SemanticClassificationService>>();

        _activeClasses =
        [
            DocumentClass.Create("RELATORIO_TECNICO_PRELIMINAR", "ADMINISTRATIVO", "PROCESSO_ADMINISTRATIVO", "Desc", isSystemDefined: true),
            DocumentClass.Create("CONTRATO_ADMINISTRATIVO", "ADMINISTRATIVO", "CONTRATOS", "Desc", isSystemDefined: true),
            DocumentClass.Create("NOTA_FISCAL", "FISCAL", "NFE", "Desc", isSystemDefined: true)
        ];

        _documentClassCatalogService.GetActiveAsync(Arg.Any<CancellationToken>())
            .Returns(_activeClasses);

        _service = new SemanticClassificationService(
            _documentClassCatalogService,
            _semanticClassifier,
            Options.Create(_options),
            logger);
    }

    [Fact]
    public async Task Should_Reuse_Existing_Class_When_Confidence_Is_Above_Threshold()
    {
        _semanticClassifier.ClassifyAsync(
                Arg.Any<string>(),
                Arg.Any<IReadOnlyCollection<DocumentClass>>(),
                Arg.Any<CancellationToken>())
            .Returns(new RawSemanticClassificationResult(
                "RELATORIO_TECNICO_PRELIMINAR",
                "ADMINISTRATIVO",
                "PROCESSO_ADMINISTRATIVO",
                ReuseExistingClass: true,
                Confidence: 0.97m));

        var result = await _service.ClassifyAsync("Relatorio Tecnico Preliminar de fiscalizacao");

        Assert.True(result.ReusedExistingClass);
        Assert.False(result.NewClassSuggested);
        Assert.NotNull(result.DocumentClassId);
        Assert.Equal(_activeClasses[0].Id, result.DocumentClassId);
        Assert.Equal("RELATORIO_TECNICO_PRELIMINAR", result.DocumentType);
        Assert.Equal(0.97m, result.Confidence);
    }

    [Fact]
    public async Task Should_Suggest_New_Class_When_Reuse_Is_False()
    {
        _semanticClassifier.ClassifyAsync(
                Arg.Any<string>(),
                Arg.Any<IReadOnlyCollection<DocumentClass>>(),
                Arg.Any<CancellationToken>())
            .Returns(new RawSemanticClassificationResult(
                "PARECER_JURIDICO",
                "JURIDICO",
                "CONSULTIVO",
                ReuseExistingClass: false,
                Confidence: 0.95m));

        var result = await _service.ClassifyAsync("Parecer juridico sobre contrato");

        Assert.False(result.ReusedExistingClass);
        Assert.True(result.NewClassSuggested);
        Assert.Null(result.DocumentClassId);
        Assert.Equal("PARECER_JURIDICO", result.DocumentType);
        Assert.Equal("JURIDICO", result.Group);
        Assert.Equal("CONSULTIVO", result.SubGroup);
        Assert.Equal(0.95m, result.Confidence);
    }

    [Fact]
    public async Task Should_Suggest_New_Class_When_Confidence_Is_Below_Threshold()
    {
        var strictOptions = new ClassificationOptions { ReuseThreshold = 0.90m };
        var strictService = new SemanticClassificationService(
            _documentClassCatalogService,
            _semanticClassifier,
            Options.Create(strictOptions),
            Substitute.For<ILogger<SemanticClassificationService>>());

        _semanticClassifier.ClassifyAsync(
                Arg.Any<string>(),
                Arg.Any<IReadOnlyCollection<DocumentClass>>(),
                Arg.Any<CancellationToken>())
            .Returns(new RawSemanticClassificationResult(
                "RELATORIO_TECNICO_PRELIMINAR",
                "ADMINISTRATIVO",
                "PROCESSO_ADMINISTRATIVO",
                ReuseExistingClass: true,
                Confidence: 0.80m));

        var result = await strictService.ClassifyAsync("Relatorio Tecnico");

        Assert.False(result.ReusedExistingClass);
        Assert.True(result.NewClassSuggested);
        Assert.Null(result.DocumentClassId);
    }

    [Fact]
    public async Task Should_Suggest_New_Class_When_Reuse_True_But_Class_Not_Found()
    {
        _semanticClassifier.ClassifyAsync(
                Arg.Any<string>(),
                Arg.Any<IReadOnlyCollection<DocumentClass>>(),
                Arg.Any<CancellationToken>())
            .Returns(new RawSemanticClassificationResult(
                "NOTA_TECNICA",
                "TECNICO",
                "NOTAS",
                ReuseExistingClass: true,
                Confidence: 0.92m));

        var result = await _service.ClassifyAsync("Nota Tecnica 123");

        Assert.False(result.ReusedExistingClass);
        Assert.True(result.NewClassSuggested);
        Assert.Null(result.DocumentClassId);
        Assert.Equal("NOTA_TECNICA", result.DocumentType);
    }

    [Fact]
    public async Task Should_Normalize_Capitalization_Variations()
    {
        _semanticClassifier.ClassifyAsync(
                Arg.Any<string>(),
                Arg.Any<IReadOnlyCollection<DocumentClass>>(),
                Arg.Any<CancellationToken>())
            .Returns(new RawSemanticClassificationResult(
                "relatorio_tecnico_preliminar",
                "ADMINISTRATIVO",
                "PROCESSO_ADMINISTRATIVO",
                ReuseExistingClass: true,
                Confidence: 0.93m));

        var result = await _service.ClassifyAsync("Relatorio Tecnico");

        Assert.True(result.ReusedExistingClass);
        Assert.NotNull(result.DocumentClassId);
        Assert.Equal(_activeClasses[0].Id, result.DocumentClassId);
    }

    [Fact]
    public async Task Should_Use_Threshold_To_Reject_Borderline_Match()
    {
        var highThresholdOptions = new ClassificationOptions { ReuseThreshold = 0.95m };
        var highThresholdService = new SemanticClassificationService(
            _documentClassCatalogService,
            _semanticClassifier,
            Options.Create(highThresholdOptions),
            Substitute.For<ILogger<SemanticClassificationService>>());

        _semanticClassifier.ClassifyAsync(
                Arg.Any<string>(),
                Arg.Any<IReadOnlyCollection<DocumentClass>>(),
                Arg.Any<CancellationToken>())
            .Returns(new RawSemanticClassificationResult(
                "NOTA_FISCAL",
                "FISCAL",
                "NFE",
                ReuseExistingClass: true,
                Confidence: 0.94m));

        var result = await highThresholdService.ClassifyAsync("nota fiscal");

        Assert.False(result.ReusedExistingClass);
        Assert.True(result.NewClassSuggested);
    }
}
