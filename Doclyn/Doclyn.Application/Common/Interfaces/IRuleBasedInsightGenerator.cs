using Doclyn.Application.Documents.Insights;

namespace Doclyn.Application.Common.Interfaces;

public interface IRuleBasedInsightGenerator
{
    IReadOnlyCollection<DocumentInsightResult> Generate(ExtractedDocumentData extractedData);
}
