using Doclyn.Application.Common.Interfaces;
using Doclyn.Application.Documents.Insights;
using Microsoft.Extensions.Logging;

namespace Doclyn.Infrastructure.Insights;

public sealed class InsightMergeService : IInsightMergeService
{
    private readonly ILogger<InsightMergeService> _logger;

    public InsightMergeService(ILogger<InsightMergeService> logger)
    {
        _logger = logger;
    }

    public IReadOnlyCollection<DocumentInsightResult> Merge(
        IReadOnlyCollection<DocumentInsightResult> ruleInsights,
        IReadOnlyCollection<DocumentInsightResult> aiInsights)
    {
        var merged = new List<DocumentInsightResult>(ruleInsights);

        foreach (var aiInsight in aiInsights)
        {
            var isDuplicate = merged.Any(r =>
                r.Type == aiInsight.Type
                && string.Equals(r.RelatedFieldName, aiInsight.RelatedFieldName, StringComparison.OrdinalIgnoreCase));

            if (!isDuplicate)
            {
                merged.Add(aiInsight);
            }
        }

        _logger.LogInformation(
            "InsightsMerged: {RuleCount} rule + {AiCount} AI = {TotalCount} total insights.",
            ruleInsights.Count,
            aiInsights.Count,
            merged.Count);

        return merged;
    }
}
