using Doclyn.Application.Common.Interfaces;
using Doclyn.Application.DocumentExtraction.ClassGuidedExtraction;
using Doclyn.Application.Documents.Processing;
using Doclyn.Domain.Entities;
using Doclyn.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Doclyn.Infrastructure.Validation;

namespace Doclyn.Infrastructure.Extraction;

public sealed class ClassGuidedExtractionService : IClassGuidedExtractionService
{
    private readonly IApplicationDbContext _context;
    private readonly IDocumentClassIndexerCatalogService _documentClassIndexerCatalogService;
    private readonly IDocumentIndexer _documentIndexer;
    private readonly IAiStructuredDataExtractor _aiStructuredDataExtractor;
    private readonly IFieldValidationService _fieldValidationService;
    private readonly FieldConfidenceOptions _confidenceOptions;
    private readonly ILogger<ClassGuidedExtractionService> _logger;

    public ClassGuidedExtractionService(
        IApplicationDbContext context,
        IDocumentClassIndexerCatalogService documentClassIndexerCatalogService,
        IDocumentIndexer documentIndexer,
        IAiStructuredDataExtractor aiStructuredDataExtractor,
        IFieldValidationService fieldValidationService,
        IOptions<FieldConfidenceOptions> confidenceOptions,
        ILogger<ClassGuidedExtractionService> logger)
    {
        _context = context;
        _documentClassIndexerCatalogService = documentClassIndexerCatalogService;
        _documentIndexer = documentIndexer;
        _aiStructuredDataExtractor = aiStructuredDataExtractor;
        _fieldValidationService = fieldValidationService;
        _confidenceOptions = confidenceOptions.Value;
        _logger = logger;
    }

    public async Task<ClassGuidedExtractionResult> ExtractAsync(
        Guid documentClassId,
        string documentText,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("ClassGuidedExtractionStarted for DocumentClassId={DocumentClassId}", documentClassId);

        var documentClass = await _context.DocumentClasses
            .AsNoTracking()
            .FirstOrDefaultAsync(dc => dc.Id == documentClassId, cancellationToken)
            ?? throw new InvalidOperationException($"Document class '{documentClassId}' not found.");

        var indexers = await _documentClassIndexerCatalogService.GetActiveByDocumentClassAsync(
            documentClassId,
            cancellationToken);

        _logger.LogInformation(
            "ClassIndexersLoaded: {IndexerCount} active indexers for class {DocumentClassName}.",
            indexers.Count,
            documentClass.Name);

        var regexResult = _documentIndexer.ExtractIndexes(documentText, indexers);

        _logger.LogInformation(
            "RegexExtractionCompleted: {RegexFieldCount} fields extracted.",
            regexResult.Count(r => HasMeaningfulValue(r.Value.Value)));

        var (missingIndexers, fields) = BuildFieldsFromRegex(regexResult, indexers);

        if (missingIndexers.Count > 0)
        {
            _logger.LogInformation(
                "DynamicPromptGenerated: {MissingCount} fields remaining for AI extraction.",
                missingIndexers.Count);

            try
            {
                var aiResult = await _aiStructuredDataExtractor.ExtractAsync(
                    documentText,
                    documentClass,
                    missingIndexers,
                    cancellationToken);

                _logger.LogInformation(
                    "AiGuidedExtractionCompleted: {AiFieldCount} fields extracted by AI.",
                    aiResult?.Count ?? 0);

                if (aiResult is not null)
                {
                    foreach (var (key, value) in aiResult)
                    {
                        if (!fields.ContainsKey(key))
                        {
                            var aiConfidence = _confidenceOptions.DefaultAiConfidence;
                            var finalValue = value;

                            if (value is Dictionary<string, object?> nested
                                && nested.TryGetValue("confidence", out var confidenceObj)
                                && confidenceObj is decimal nestedConfidence)
                            {
                                aiConfidence = nestedConfidence;
                                finalValue = nested.GetValueOrDefault("value");
                            }
                            else if (value is Dictionary<string, object?> nested2
                                && nested2.TryGetValue("confidence", out var confidenceObj2)
                                && confidenceObj2 is long longConfidence)
                            {
                                aiConfidence = longConfidence;
                                finalValue = nested2.GetValueOrDefault("value");
                            }

                            var status = _fieldValidationService.DetermineStatus(aiConfidence);

                            fields[key] = new ExtractedFieldResult(
                                finalValue,
                                aiConfidence,
                                ExtractionSource.AI,
                                status);

                            _logger.LogInformation("FieldMerged: {FieldName} filled by AI (confidence={Confidence}, status={Status}).", key, aiConfidence, status);
                            _logger.LogInformation("ConfidenceCalculated: {FieldName} confidence={Confidence}.", key, aiConfidence);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "AiGuidedExtractionCompleted with failure: AI extraction skipped.");
            }
        }

        foreach (var indexer in missingIndexers)
        {
            if (indexer.IsRequired && !fields.ContainsKey(indexer.Name))
            {
                fields[indexer.Name] = new ExtractedFieldResult(
                    null,
                    0m,
                    ExtractionSource.AI,
                    ValidationStatus.Rejected);

                _logger.LogInformation("FieldRejected: {FieldName} is required but not found.", indexer.Name);
            }
        }

        foreach (var (key, field) in fields)
        {
            var logStep = field.ValidationStatus switch
            {
                ValidationStatus.Validated => "FieldValidated",
                ValidationStatus.NeedsReview => "FieldNeedsReview",
                ValidationStatus.Rejected => "FieldRejected",
                _ => null
            };

            if (logStep is not null)
            {
                _logger.LogInformation("{Step}: {FieldName} (confidence={Confidence}, status={Status}).",
                    logStep, key, field.Confidence, field.ValidationStatus);
            }
        }

        _logger.LogInformation(
            "ClassGuidedExtractionCompleted: {TotalFields} fields in result for class {DocumentClassName}.",
            fields.Count,
            documentClass.Name);

        return new ClassGuidedExtractionResult(documentClassId, fields);
    }

    private (List<DocumentClassIndexer> MissingIndexers, Dictionary<string, ExtractedFieldResult> Fields)
        BuildFieldsFromRegex(
            Dictionary<string, DocumentIndexerValue> regexResult,
            IReadOnlyCollection<DocumentClassIndexer> indexers)
    {
        var fields = new Dictionary<string, ExtractedFieldResult>(StringComparer.OrdinalIgnoreCase);
        var missing = new List<DocumentClassIndexer>();

        foreach (var indexer in indexers)
        {
            if (regexResult.TryGetValue(indexer.Name, out var indexerValue) && HasMeaningfulValue(indexerValue.Value))
            {
                fields[indexer.Name] = new ExtractedFieldResult(
                    indexerValue.Value,
                    (decimal)indexerValue.Confidence,
                    ExtractionSource.Regex,
                    ValidationStatus.Validated);
            }
            else
            {
                missing.Add(indexer);
            }
        }

        return (missing, fields);
    }

    private static bool HasMeaningfulValue(object? value)
    {
        return value switch
        {
            null => false,
            string text => !string.IsNullOrWhiteSpace(text),
            Array array => array.Length > 0,
            _ => true
        };
    }
}
