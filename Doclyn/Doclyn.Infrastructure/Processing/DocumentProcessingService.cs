using System.Globalization;
using System.Text;
using System.Text.Json;
using Doclyn.Application.Common.Interfaces;
using Doclyn.Application.DocumentExtraction.ClassGuidedExtraction;
using Doclyn.Application.Documents.Insights;
using Doclyn.Application.Documents.Processing;
using Doclyn.Domain.Entities;
using Doclyn.Domain.Enums;
using Doclyn.Infrastructure.AI;
using Doclyn.Infrastructure.OCR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Doclyn.Infrastructure.Processing;

public sealed class DocumentProcessingService : IDocumentProcessingService
{
    private static readonly string[] MinimumKeywords =
    [
        "PROCESSO ADMINISTRATIVO",
        "RELATORIO TECNICO",
        "CONTRATO",
        "PREFEITURA",
        "CNPJ"
    ];

    private readonly IApplicationDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
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
    private readonly ExtractionMergeService _extractionMergeService;
    private readonly OcrOptions _ocrOptions;
    private readonly OcrProcessingContextAccessor _ocrProcessingContextAccessor;
    private readonly DocumentTextMergeService _documentTextMergeService;
    private readonly ILogger<DocumentProcessingService> _logger;

    public DocumentProcessingService(
        IApplicationDbContext context,
        IUnitOfWork unitOfWork,
        IFileStorageService fileStorageService,
        IPdfTextExtractor pdfTextExtractor,
        IOcrService ocrService,
        IDocumentClassifier documentClassifier,
        IDocumentIndexer documentIndexer,
        IAiDocumentClassifier aiDocumentClassifier,
        IAiStructuredDataExtractor aiStructuredDataExtractor,
        IDocumentClassCatalogService documentClassCatalogService,
        IDocumentClassIndexerCatalogService documentClassIndexerCatalogService,
        IDocumentSemanticClassificationService documentSemanticClassificationService,
        IClassGuidedExtractionService classGuidedExtractionService,
        IDocumentInsightService documentInsightService,
        ExtractionMergeService extractionMergeService,
        IOptions<OcrOptions> ocrOptions,
        OcrProcessingContextAccessor ocrProcessingContextAccessor,
        DocumentTextMergeService documentTextMergeService,
        ILogger<DocumentProcessingService> logger)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _fileStorageService = fileStorageService;
        _pdfTextExtractor = pdfTextExtractor;
        _ocrService = ocrService;
        _documentClassifier = documentClassifier;
        _documentIndexer = documentIndexer;
        _aiDocumentClassifier = aiDocumentClassifier;
        _aiStructuredDataExtractor = aiStructuredDataExtractor;
        _documentClassCatalogService = documentClassCatalogService;
        _documentClassIndexerCatalogService = documentClassIndexerCatalogService;
        _documentSemanticClassificationService = documentSemanticClassificationService;
        _classGuidedExtractionService = classGuidedExtractionService;
        _documentInsightService = documentInsightService;
        _extractionMergeService = extractionMergeService;
        _ocrOptions = ocrOptions.Value;
        _ocrProcessingContextAccessor = ocrProcessingContextAccessor;
        _documentTextMergeService = documentTextMergeService;
        _logger = logger;
    }
     
    public async Task ProcessAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        var document = await _context.Documents.FirstOrDefaultAsync(d => d.Id == documentId, cancellationToken)
            ?? throw new InvalidOperationException("Document not found.");

        if (document.DocumentStatus == DocumentStatus.Processing)
        {
            _logger.LogInformation("Skipping document {DocumentId} because it is already processing.", documentId);
            return;
        }

        if (document.DocumentStatus is not DocumentStatus.Pending and not DocumentStatus.Failed and not DocumentStatus.Processed)
        {
            throw new InvalidOperationException("Document cannot be processed in its current state.");
        }

        try
        {
            document.UpdateStatus(DocumentStatus.Processing);
            await _unitOfWork.CommitAsync(cancellationToken);

            await AddLogAsync(document.Id, "ProcessingStarted", "Document processing started.", DocumentStatus.Processing, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);

            var pdfBytes = await DownloadPdfBytesAsync(document.StoragePath, cancellationToken);
            var nativeText = await ExtractTextAsync(pdfBytes, cancellationToken);
            string? ocrText = null;
            var ocrWasUsed = false;

            if (!_ocrOptions.Enabled)
            {
                await FailAsync(document, "OCR is required for processing, but OCR is disabled.", cancellationToken);
                return;
            }

            ocrWasUsed = true;
            await AddLogAsync(document.Id, "OcrStarted", "OCR started for this document.", DocumentStatus.Processing, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);

            _ocrProcessingContextAccessor.OnPageProcessedAsync = async (pageNumber, token) =>
            {
                await AddLogAsync(
                    document.Id,
                    "OcrPageProcessed",
                    $"OCR processed page {pageNumber}.",
                    DocumentStatus.Processing,
                    token);
                await _unitOfWork.CommitAsync(token);
            };

            try
            {
                ocrText = await ExtractTextWithOcrAsync(pdfBytes, cancellationToken);
                var ocrQuality = OcrTextQualityEvaluator.Evaluate(ocrText);
                _logger.LogInformation(
                    "OcrQuality documentId:{DocumentId} length:{Length} lines:{Lines} alphaRatio:{AlphaRatio:F2} nonEmptyLines:{NonEmptyLines} quality:{Quality}",
                    document.Id,
                    ocrText.Length,
                    ocrQuality.TotalLines,
                    ocrQuality.AlphaRatio,
                    ocrQuality.NonEmptyLines,
                    ocrQuality.Quality);
                await AddLogAsync(document.Id, "OcrCompleted", $"OCR completed. Quality: {ocrQuality.NonEmptyLines} non-empty lines, {ocrText.Length} chars ({ocrQuality.Quality}).", DocumentStatus.Success, cancellationToken);
                await _unitOfWork.CommitAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                await AddLogAsync(document.Id, "OcrFailed", ex.Message, DocumentStatus.Failed, cancellationToken);
                await _unitOfWork.CommitAsync(cancellationToken);
                await FailAsync(document, "OCR failed for this document.", cancellationToken);
                return;
            }
            finally
            {
                _ocrProcessingContextAccessor.Reset();
            }

            var textBundle = _documentTextMergeService.Merge(nativeText, ocrText, ocrWasUsed);
            var mergedText = textBundle.MergedText;

            _logger.LogInformation(
                "TextMerge documentId:{DocumentId} nativeLen:{NativeLen} ocrLen:{OcrLen} mergedLen:{MergedLen} ocrUsed:{OcrUsed}",
                document.Id,
                textBundle.NativeText.Length,
                textBundle.OcrText?.Length ?? 0,
                mergedText.Length,
                ocrWasUsed);

            if (!OcrTextQualityEvaluator.TextLooksSufficient(mergedText, _ocrOptions.MinimumTextLength, MinimumKeywords))
            {
                await FailAsync(document, "Merged text from native extraction and OCR is still insufficient for processing.", cancellationToken);
                return;
            }

            await AddLogAsync(document.Id, "TextExtracted", $"Text extracted from PDF successfully. Sources: native={textBundle.NativeText.Length} chars, ocr={textBundle.OcrText?.Length ?? 0} chars, merged={mergedText.Length} chars.", DocumentStatus.Success, cancellationToken);

            var (documentClass, classification, semanticReuse) = await TryClassifySemanticallyAsync(
                document.Id,
                mergedText,
                cancellationToken);

            if (documentClass is null)
            {
                await FailAsync(document, "Document could not be classified. Classification is required for processing.", cancellationToken);
                return;
            }

            if (documentClass.Name == "DOCUMENTO_DESCONHECIDO")
            {
                _logger.LogWarning(
                    "ClassificationInsufficient documentId:{DocumentId} classification:{DocumentType} confidence:{Confidence}",
                    documentId,
                    classification.DocumentType,
                    classification.Confidence);
                await AddLogAsync(document.Id, "ClassificationFailed",
                    $"Document could not be classified reliably. Classification is required for processing. Got: {classification.DocumentType} (confidence: {classification.Confidence}).",
                    DocumentStatus.Failed, cancellationToken);
                await _unitOfWork.CommitAsync(cancellationToken);
                await FailAsync(document, "Document could not be classified. Classification is required for processing.", cancellationToken);
                return;
            }

            var indexers = await _documentClassIndexerCatalogService.GetActiveByDocumentClassAsync(
                documentClass.Id,
                cancellationToken);

            await AddLogAsync(
                document.Id,
                "DocumentClassIndexersLoaded",
                $"{indexers.Count} indexers loaded for class {documentClass.Name}.",
                DocumentStatus.Success,
                cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);

            await AddLogAsync(document.Id, "DocumentClassified", $"Document classified as {classification.DocumentType}.", DocumentStatus.Success, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);

            var guidedExtraction = await _classGuidedExtractionService.ExtractAsync(
                documentClass.Id,
                mergedText,
                cancellationToken);

            var catalogExtraction = _documentIndexer.ExtractIndexes(mergedText, indexers);
            var regexExtraction = BuildRegexExtraction(catalogExtraction, classification);

            await AddLogAsync(document.Id, "RegexIndexesExtracted", "Regex indexes extracted from catalog.", DocumentStatus.Success, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);

            var aiExtraction = await TryExtractWithAiAsync(document.Id, mergedText, documentClass, indexers, cancellationToken);
            var indexersSection = BuildMergedIndexersSection(catalogExtraction, aiExtraction);
            var finalResult = _extractionMergeService.Merge(classification, regexExtraction, aiExtraction);

            await AddLogAsync(document.Id, "IndexesExtracted", "Indexes extracted successfully.", DocumentStatus.Success, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);

            var usefulFieldCount = CountUsefulFields(guidedExtraction);
            var validatedCount = CountFieldsByStatus(guidedExtraction, ValidationStatus.Validated);
            var needsReviewCount = CountFieldsByStatus(guidedExtraction, ValidationStatus.NeedsReview);
            var rejectedCount = CountFieldsByStatus(guidedExtraction, ValidationStatus.Rejected);

            var payload = JsonDocument.Parse(JsonSerializer.Serialize(new Dictionary<string, object?>
            {
                ["classification"] = new Dictionary<string, object?>
                {
                    ["documentClassId"] = documentClass.Id,
                    ["documentType"] = classification.DocumentType,
                    ["group"] = classification.Group,
                    ["subGroup"] = classification.Subgroup,
                    ["confidence"] = classification.Confidence,
                    ["reusedExistingClass"] = semanticReuse
                },
                ["fields"] = BuildCanonicalFields(guidedExtraction),
                ["indexers"] = indexersSection,
                ["regexExtraction"] = regexExtraction,
                ["aiExtraction"] = aiExtraction,
                ["finalResult"] = finalResult,
                ["textSources"] = new Dictionary<string, object?>
                {
                    ["nativeTextAvailable"] = !string.IsNullOrWhiteSpace(textBundle.NativeText),
                    ["ocrTextAvailable"] = !string.IsNullOrWhiteSpace(textBundle.OcrText),
                    ["nativeLength"] = textBundle.NativeText.Length,
                    ["ocrLength"] = textBundle.OcrText?.Length ?? 0,
                    ["mergedLength"] = textBundle.MergedText.Length,
                    ["ocrUsed"] = textBundle.OcrUsed
                },
                ["processingQuality"] = new Dictionary<string, object?>
                {
                    ["usefulFields"] = usefulFieldCount,
                    ["validatedFields"] = validatedCount,
                    ["needsReviewFields"] = needsReviewCount,
                    ["rejectedFields"] = rejectedCount,
                    ["ocrUsed"] = ocrWasUsed,
                    ["quality"] = usefulFieldCount >= 3 ? "good" : usefulFieldCount >= 1 ? "low" : "insufficient"
                }
            }));

            var extractedData = await _context.ExtractedData.FirstOrDefaultAsync(ed => ed.DocumentId == document.Id, cancellationToken);
            if (extractedData is null)
            {
                _context.ExtractedData.Add(ExtractedData.Create(document.Id, payload));
            }
            else
            {
                extractedData.UpdateData(payload);
            }

            var insights = await GenerateInsightsAsync(document, documentClass, guidedExtraction, mergedText, cancellationToken);

            var existingInsights = _context.DocumentInsights
                .Where(di => di.DocumentId == document.Id);
            foreach (var existing in existingInsights)
            {
                _context.DocumentInsights.Remove(existing);
            }

            foreach (var insight in insights)
            {
                _context.DocumentInsights.Add(DocumentInsight.Create(
                    document.Id,
                    insight.Type,
                    insight.Severity,
                    insight.Title,
                    insight.Message,
                    insight.Confidence,
                    insight.Source,
                    insight.RelatedFieldName));
            }

            await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "ExtractionQuality documentId:{DocumentId} useful:{UsefulFields} validated:{Validated} needsReview:{NeedsReview} rejected:{Rejected}",
                documentId,
                usefulFieldCount,
                validatedCount,
                needsReviewCount,
                rejectedCount);

            if (usefulFieldCount == 0)
            {
                await AddLogAsync(document.Id, "ExtractionInsufficient",
                    "No useful fields were extracted from the document. Processing quality is insufficient.",
                    DocumentStatus.Failed, cancellationToken);
                await _unitOfWork.CommitAsync(cancellationToken);
                await FailAsync(document, "No useful fields were extracted. Processing completed but extraction quality is insufficient.", cancellationToken);
                return;
            }

            var totalFields = guidedExtraction.Fields.Count;
            var validatedRatio = totalFields > 0 ? (double)validatedCount / totalFields : 0;

            if (usefulFieldCount <= 2 && validatedRatio < 0.5)
            {
                await AddLogAsync(document.Id, "ExtractionLowQuality",
                    $"Only {usefulFieldCount} useful field(s) extracted ({validatedCount} validated, {needsReviewCount} needs review, {rejectedCount} rejected). Manual review recommended.",
                    DocumentStatus.Success, cancellationToken);
                await _unitOfWork.CommitAsync(cancellationToken);
            }

            document.UpdateDocumentType(classification.DocumentType);
            document.UpdateStatus(DocumentStatus.Processed);

            await AddLogAsync(document.Id, "ProcessingCompleted", "Document processed successfully.", DocumentStatus.Processed, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process document {DocumentId}", documentId);
            var shouldRethrow = ex is not InvalidOperationException;

            if (document.DocumentStatus != DocumentStatus.Failed)
            {
                document.UpdateStatus(DocumentStatus.Failed);
                await AddLogAsync(document.Id, "ProcessingFailed", ex.Message, DocumentStatus.Failed, cancellationToken);
                await _unitOfWork.CommitAsync(cancellationToken);
            }

            if (shouldRethrow)
            {
                throw;
            }
        }
    }

    private async Task<IReadOnlyCollection<DocumentInsightResult>> GenerateInsightsAsync(
        Document document,
        DocumentClass documentClass,
        ClassGuidedExtractionResult guidedExtraction,
        string text,
        CancellationToken cancellationToken)
    {
        await AddLogAsync(document.Id, "InsightGenerationStarted", "Insight generation started.", DocumentStatus.Processing, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        try
        {
            var extractedData = new ExtractedDocumentData(
                document.Id,
                documentClass.Id,
                documentClass.Name,
                guidedExtraction.Fields,
                text);

            var insights = await _documentInsightService.GenerateAsync(
                document.Id,
                extractedData,
                cancellationToken);

            await AddLogAsync(document.Id, "InsightGenerationCompleted",
                $"Generated {insights.Count} insights for document.",
                DocumentStatus.Success, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);

            return insights;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "InsightGenerationFailed for document {DocumentId}", document.Id);
            await AddLogAsync(document.Id, "InsightGenerationFailed",
                "Insight generation failed. Document processing continues.",
                DocumentStatus.Success, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);

            return [];
        }
    }

    private async Task<(DocumentClass? DocumentClass, DocumentClassificationResult Classification, bool SemanticReuse)> TryClassifySemanticallyAsync(
        Guid documentId,
        string text,
        CancellationToken cancellationToken)
    {
        await AddLogAsync(documentId, "SemanticClassificationStarted", "Semantic classification started.", DocumentStatus.Processing, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        try
        {
            var semanticResult = await _documentSemanticClassificationService.ClassifyAsync(text, cancellationToken);

            if (semanticResult.ReusedExistingClass && semanticResult.DocumentClassId.HasValue)
            {
                var existingClass = await _documentClassCatalogService.FindByNameAsync(
                    semanticResult.DocumentType,
                    cancellationToken);

                if (existingClass is not null)
                {
                    await _documentClassCatalogService.RegisterExampleAsync(
                        existingClass.Id,
                        documentId,
                        semanticResult.Confidence,
                        cancellationToken);

                    await AddLogAsync(documentId, "ExistingClassMatched", $"Class {existingClass.Name} matched semantically.", DocumentStatus.Success, cancellationToken);
                    await _unitOfWork.CommitAsync(cancellationToken);

                    await AddLogAsync(documentId, "DocumentClassReused", $"Class {existingClass.Name} reused.", DocumentStatus.Success, cancellationToken);
                    await _unitOfWork.CommitAsync(cancellationToken);

                    await AddLogAsync(documentId, "SemanticClassificationCompleted", "Semantic classification completed (reused).", DocumentStatus.Success, cancellationToken);
                    await _unitOfWork.CommitAsync(cancellationToken);

                    _logger.LogInformation(
                        "DocumentClassReused: {DocumentClassName} for document {DocumentId} (semantic).",
                        existingClass.Name,
                        documentId);

                    var classification = new DocumentClassificationResult(
                        existingClass.Name,
                        existingClass.Group,
                        existingClass.SubGroup,
                        (double)semanticResult.Confidence);

                    return (existingClass, classification, true);
                }
            }

            if (semanticResult.NewClassSuggested)
            {
                var newClass = await _documentClassCatalogService.GetOrCreateAsync(
                    semanticResult.DocumentType,
                    semanticResult.Group,
                    semanticResult.SubGroup,
                    "Document class suggested by semantic classification.",
                    cancellationToken);

                await _documentClassCatalogService.RegisterExampleAsync(
                    newClass.Id,
                    documentId,
                    semanticResult.Confidence,
                    cancellationToken);

                await AddLogAsync(documentId, "NewClassSuggested", $"New class suggested: {semanticResult.DocumentType}.", DocumentStatus.Success, cancellationToken);
                await _unitOfWork.CommitAsync(cancellationToken);

                await AddLogAsync(documentId, "DocumentClassCreated", $"New class created: {newClass.Name}.", DocumentStatus.Success, cancellationToken);
                await _unitOfWork.CommitAsync(cancellationToken);

                await AddLogAsync(documentId, "SemanticClassificationCompleted", "Semantic classification completed (new class).", DocumentStatus.Success, cancellationToken);
                await _unitOfWork.CommitAsync(cancellationToken);

                _logger.LogInformation(
                    "DocumentClassCreated: {DocumentClassName} for document {DocumentId} (semantic).",
                    newClass.Name,
                    documentId);

                var classification = new DocumentClassificationResult(
                    newClass.Name,
                    newClass.Group,
                    newClass.SubGroup,
                    (double)semanticResult.Confidence);

                return (newClass, classification, false);
            }

            _logger.LogWarning(
                "Semantic classification could not determine document class for document {DocumentId}.",
                documentId);
            await AddLogAsync(documentId, "ClassificationFailed",
                "Semantic classification could not determine a document class.",
                DocumentStatus.Failed, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);

            return (null, new DocumentClassificationResult("UNKNOWN", "UNKNOWN", "UNKNOWN", 0.0), false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Semantic classification failed for document {DocumentId}.", documentId);
            await AddLogAsync(documentId, "ClassificationFailed",
                "Semantic classification failed. Document cannot be processed without classification.",
                DocumentStatus.Failed, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);

            return (null, new DocumentClassificationResult("UNKNOWN", "UNKNOWN", "UNKNOWN", 0.0), false);
        }
    }

    private async Task<(DocumentClass DocumentClass, DocumentClassificationResult Classification, bool SemanticReuse)> FallbackClassifyAsync(
        Guid documentId,
        string text,
        CancellationToken cancellationToken)
    {
        var fallbackClassification = _documentClassifier.Classify(text);
        var classification = await TryClassifyWithAiAsync(documentId, text, fallbackClassification, cancellationToken);

        var documentClass = await _documentClassCatalogService.GetOrCreateAsync(
            classification.DocumentType,
            classification.Group,
            classification.Subgroup,
            "Document class learned from processing.",
            cancellationToken);

        await _documentClassCatalogService.RegisterExampleAsync(
            documentClass.Id,
            documentId,
            (decimal)(classification.Confidence ?? 0.0),
            cancellationToken);

        _logger.LogInformation(
            "DocumentClassFound: {DocumentClassName} reused for document {DocumentId}.",
            documentClass.Name,
            documentId);

        await AddLogAsync(
            documentId,
            "DocumentClassFound",
            $"Class {documentClass.Name} reused.",
            DocumentStatus.Success,
            cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        return (documentClass, classification, false);
    }

    private async Task<DocumentClassificationResult> TryClassifyWithAiAsync(
        Guid documentId,
        string text,
        DocumentClassificationResult fallbackClassification,
        CancellationToken cancellationToken)
    {
        await AddLogAsync(documentId, "AiClassificationStarted", "AI classification started.", DocumentStatus.Processing, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        try
        {
            var aiClassification = await _aiDocumentClassifier.ClassifyAsync(text, cancellationToken);
            await AddLogAsync(documentId, "AiClassificationCompleted", "AI classification completed successfully.", DocumentStatus.Success, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);

            return string.IsNullOrWhiteSpace(aiClassification.DocumentType)
                ? fallbackClassification
                : aiClassification;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AI classification failed for document {DocumentId}", documentId);
            await AddLogAsync(documentId, "AiClassificationFailed", "AI unavailable. Partial extraction completed.", DocumentStatus.Success, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);
            return fallbackClassification;
        }
    }

    private async Task<Dictionary<string, object?>?> TryExtractWithAiAsync(
        Guid documentId,
        string text,
        DocumentClass documentClass,
        IReadOnlyCollection<DocumentClassIndexer> indexers,
        CancellationToken cancellationToken)
    {
        await AddLogAsync(documentId, "AiExtractionStarted", "AI extraction started.", DocumentStatus.Processing, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        try
        {
            var extraction = await _aiStructuredDataExtractor.ExtractAsync(text, documentClass, indexers, cancellationToken);

            if (extraction is null || extraction.Count == 0)
            {
                await AddLogAsync(documentId, "AiExtractionEmpty", "AI extraction completed but returned no fields.", DocumentStatus.Success, cancellationToken);
            }
            else
            {
                await AddLogAsync(documentId, "AiExtractionCompleted", $"AI extraction completed with {extraction.Count} fields.", DocumentStatus.Success, cancellationToken);
            }

            await _unitOfWork.CommitAsync(cancellationToken);
            return extraction;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AI extraction failed for document {DocumentId}", documentId);
            await AddLogAsync(documentId, "AiExtractionFailed", "AI unavailable. Partial extraction completed.", DocumentStatus.Success, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);
            return null;
        }
    }

    private async Task FailAsync(Document document, string message, CancellationToken cancellationToken)
    {
        document.UpdateStatus(DocumentStatus.Failed);
        await AddLogAsync(document.Id, "ProcessingFailed", message, DocumentStatus.Failed, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
    }

    private async Task<byte[]> DownloadPdfBytesAsync(string storagePath, CancellationToken cancellationToken)
    {
        await using var pdfStream = await _fileStorageService.DownloadAsync(storagePath, cancellationToken);
        using var memoryStream = new MemoryStream();
        await pdfStream.CopyToAsync(memoryStream, cancellationToken);
        return memoryStream.ToArray();
    }

    private async Task<string> ExtractTextAsync(byte[] pdfBytes, CancellationToken cancellationToken)
    {
        using var stream = new MemoryStream(pdfBytes, writable: false);
        return await _pdfTextExtractor.ExtractTextAsync(stream, cancellationToken);
    }

    private async Task<string> ExtractTextWithOcrAsync(byte[] pdfBytes, CancellationToken cancellationToken)
    {
        using var stream = new MemoryStream(pdfBytes, writable: false);
        return await _ocrService.ExtractTextAsync(stream, cancellationToken);
    }

    private static Dictionary<string, object?> BuildRegexExtraction(
        Dictionary<string, DocumentIndexerValue> catalogExtraction,
        DocumentClassificationResult classification)
    {
        var regexExtraction = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["documentType"] = classification.DocumentType,
            ["group"] = classification.Group,
            ["subGroup"] = classification.Subgroup
        };

        foreach (var (key, indexerValue) in catalogExtraction)
        {
            regexExtraction[key] = indexerValue.Value;
        }

        return regexExtraction;
    }

    private static Dictionary<string, object?> BuildMergedIndexersSection(
        Dictionary<string, DocumentIndexerValue> catalogExtraction,
        Dictionary<string, object?>? aiExtraction)
    {
        var indexersSection = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        foreach (var (key, indexerValue) in catalogExtraction)
        {
            indexersSection[key] = new Dictionary<string, object?>
            {
                ["value"] = indexerValue.Value,
                ["source"] = indexerValue.Source,
                ["confidence"] = indexerValue.Confidence
            };
        }

        if (aiExtraction is not null)
        {
            foreach (var (key, value) in aiExtraction)
            {
                if (!indexersSection.ContainsKey(key))
                {
                    indexersSection[key] = new Dictionary<string, object?>
                    {
                        ["value"] = value,
                        ["source"] = "AI",
                        ["confidence"] = 0.8
                    };
                }
            }
        }

        return indexersSection;
    }

    private static Dictionary<string, object?> BuildCanonicalFields(ClassGuidedExtractionResult guidedExtraction)
    {
        var fieldsDict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        foreach (var (key, field) in guidedExtraction.Fields)
        {
            fieldsDict[key] = new Dictionary<string, object?>
            {
                ["value"] = field.Value,
                ["confidence"] = field.Confidence,
                ["source"] = field.Source.ToString(),
                ["validationStatus"] = field.ValidationStatus.ToString()
            };
        }

        return fieldsDict;
    }

    private Task AddLogAsync(
        Guid documentId,
        string step,
        string message,
        DocumentStatus status,
        CancellationToken cancellationToken)
    {
        _context.ProcessingLogs.Add(ProcessingLog.Create(documentId, step, message, status));
        _logger.LogInformation("{Step} for document {DocumentId}: {Message}", step, documentId, message);
        return Task.CompletedTask;
    }

    private static int CountUsefulFields(ClassGuidedExtractionResult guidedExtraction)
    {
        return guidedExtraction.Fields.Values.Count(f => HasMeaningfulValue(f.Value));
    }

    private static int CountFieldsByStatus(ClassGuidedExtractionResult guidedExtraction, ValidationStatus status)
    {
        return guidedExtraction.Fields.Values.Count(f => f.ValidationStatus == status);
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

    private static (int TotalLines, int NonEmptyLines, double AlphaRatio) EvaluateOcrQuality(string text)
    {
        var result = OcrTextQualityEvaluator.Evaluate(text);
        return (result.TotalLines, result.NonEmptyLines, result.AlphaRatio);
    }
}
