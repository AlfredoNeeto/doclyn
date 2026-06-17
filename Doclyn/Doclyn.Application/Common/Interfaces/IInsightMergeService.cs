using Doclyn.Application.Documents.Insights;

namespace Doclyn.Application.Common.Interfaces;

public interface IInsightMergeService
{
    IReadOnlyCollection<DocumentInsightResult> Merge(
        IReadOnlyCollection<DocumentInsightResult> ruleInsights,
        IReadOnlyCollection<DocumentInsightResult> aiInsights);
}
