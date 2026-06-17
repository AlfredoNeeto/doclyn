using Doclyn.Application.Documents.Insights;

namespace Doclyn.Application.Common.Interfaces;

public interface IAiInsightGenerator
{
    Task<IReadOnlyCollection<DocumentInsightResult>> GenerateAsync(
        string documentText,
        ExtractedDocumentData extractedData,
        CancellationToken cancellationToken = default);
}
