using Doclyn.Application.Common.Interfaces;
using Doclyn.Application.Documents.Processing;
using Doclyn.Domain.Entities;
using Doclyn.Infrastructure.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Doclyn.Infrastructure.Classification;

public sealed class SemanticClassificationService : IDocumentSemanticClassificationService
{
    private readonly IDocumentClassCatalogService _documentClassCatalogService;
    private readonly OpenAiSemanticClassifier _semanticClassifier;
    private readonly ClassificationOptions _options;
    private readonly ILogger<SemanticClassificationService> _logger;

    public SemanticClassificationService(
        IDocumentClassCatalogService documentClassCatalogService,
        OpenAiSemanticClassifier semanticClassifier,
        IOptions<ClassificationOptions> options,
        ILogger<SemanticClassificationService> logger)
    {
        _documentClassCatalogService = documentClassCatalogService;
        _semanticClassifier = semanticClassifier;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<SemanticClassificationResult> ClassifyAsync(
        string extractedText,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Semantic classification started.");

        try
        {
            var activeClasses = await _documentClassCatalogService.GetActiveAsync(cancellationToken);

            _logger.LogInformation(
                "Loaded {ActiveClassCount} active document classes for semantic classification.",
                activeClasses.Count);

            var rawResult = await _semanticClassifier.ClassifyAsync(
                extractedText,
                activeClasses,
                cancellationToken);

            var normalizedName = DocumentClass.NormalizeName(rawResult.DocumentClassName);

            _logger.LogInformation(
                "AI returned: {DocumentClassName}, reuse={Reuse}, confidence={Confidence}",
                normalizedName,
                rawResult.ReuseExistingClass,
                rawResult.Confidence);

            if (rawResult.ReuseExistingClass && rawResult.Confidence >= _options.ReuseThreshold)
            {
                var existingClass = activeClasses.FirstOrDefault(
                    dc => dc.Name == normalizedName);

                if (existingClass is not null)
                {
                    _logger.LogInformation(
                        "Existing class matched: {DocumentClassName}.",
                        existingClass.Name);

                    return new SemanticClassificationResult(
                        existingClass.Id,
                        existingClass.Name,
                        existingClass.Group,
                        existingClass.SubGroup,
                        rawResult.Confidence,
                        ReusedExistingClass: true,
                        NewClassSuggested: false);
                }

                _logger.LogWarning(
                    "AI returned reuse for '{DocumentClassName}' but no matching class found. Treating as new class suggestion.",
                    normalizedName);
            }

            var effectiveType = string.IsNullOrWhiteSpace(normalizedName)
                ? "UNKNOWN"
                : normalizedName;

            _logger.LogInformation(
                "New class suggested: {DocumentClassName}.",
                effectiveType);

            return new SemanticClassificationResult(
                DocumentClassId: null,
                effectiveType,
                rawResult.Group,
                rawResult.SubGroup,
                rawResult.Confidence,
                ReusedExistingClass: false,
                NewClassSuggested: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Semantic classification failed.");
            throw;
        }
    }
}
