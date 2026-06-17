using Doclyn.Application.Documents.Insights;

namespace Doclyn.Application.Common.Interfaces;

public interface IDocumentInsightService
{
    Task<IReadOnlyCollection<DocumentInsightResult>> GenerateAsync(
        Guid documentId,
        ExtractedDocumentData extractedData,
        CancellationToken cancellationToken = default);
}
