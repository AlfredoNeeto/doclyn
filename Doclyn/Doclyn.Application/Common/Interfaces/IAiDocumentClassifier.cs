using Doclyn.Application.Documents.Processing;

namespace Doclyn.Application.Common.Interfaces;

public interface IAiDocumentClassifier
{
    Task<DocumentClassificationResult> ClassifyAsync(string text, CancellationToken cancellationToken = default);
}
