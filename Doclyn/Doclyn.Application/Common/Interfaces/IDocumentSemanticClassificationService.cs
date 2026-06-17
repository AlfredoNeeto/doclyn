using Doclyn.Application.Documents.Processing;

namespace Doclyn.Application.Common.Interfaces;

public interface IDocumentSemanticClassificationService
{
    Task<SemanticClassificationResult> ClassifyAsync(
        string extractedText,
        CancellationToken cancellationToken = default);
}
