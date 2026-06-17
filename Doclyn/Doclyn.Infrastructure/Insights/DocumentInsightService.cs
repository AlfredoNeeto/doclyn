using Doclyn.Application.Common.Interfaces;
using Doclyn.Application.Documents.Insights;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Doclyn.Infrastructure.Insights;

public sealed class DocumentInsightService : IDocumentInsightService
{
    private readonly IRuleBasedInsightGenerator _ruleBasedGenerator;
    private readonly IAiInsightGenerator _aiGenerator;
    private readonly IInsightMergeService _mergeService;
    private readonly InsightOptions _options;
    private readonly ILogger<DocumentInsightService> _logger;

    public DocumentInsightService(
        IRuleBasedInsightGenerator ruleBasedGenerator,
        IAiInsightGenerator aiGenerator,
        IInsightMergeService mergeService,
        IOptions<InsightOptions> options,
        ILogger<DocumentInsightService> logger)
    {
        _ruleBasedGenerator = ruleBasedGenerator;
        _aiGenerator = aiGenerator;
        _mergeService = mergeService;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<IReadOnlyCollection<DocumentInsightResult>> GenerateAsync(
        Guid documentId,
        ExtractedDocumentData extractedData,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation(
                "InsightGenerationSkipped: insights are disabled for document {DocumentId}.",
                documentId);
            return [];
        }

        _logger.LogInformation(
            "InsightGenerationStarted for document {DocumentId}.", documentId);

        try
        {
            var ruleInsights = _ruleBasedGenerator.Generate(extractedData);

            _logger.LogInformation(
                "RuleInsightsGenerated: {Count} rule insights for document {DocumentId}.",
                ruleInsights.Count,
                documentId);

            IReadOnlyCollection<DocumentInsightResult> aiInsights = [];

            if (_options.EnableAiInsights && !string.IsNullOrWhiteSpace(extractedData.DocumentText))
            {
                try
                {
                    aiInsights = await _aiGenerator.GenerateAsync(
                        extractedData.DocumentText!,
                        extractedData,
                        cancellationToken);

                    if (aiInsights.Count == 0)
                    {
                        _logger.LogInformation(
                            "AiInsightsEmpty: AI returned no insights for document {DocumentId}.",
                            documentId);
                    }
                    else
                    {
                        _logger.LogInformation(
                            "AiInsightsGenerated: {Count} AI insights for document {DocumentId}.",
                            new object?[] { aiInsights.Count, documentId });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "AiInsightsFailed: AI insight generation failed for document {DocumentId}. Continuing with rule insights only.",
                        documentId);
                }
            }

            var merged = _mergeService.Merge(ruleInsights, aiInsights);

            _logger.LogInformation(
                "InsightGenerationCompleted: {Count} total insights for document {DocumentId}.",
                merged.Count,
                documentId);

            return merged;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "InsightGenerationFailed for document {DocumentId}.", documentId);
            throw;
        }
    }
}
