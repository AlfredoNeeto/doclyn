using Doclyn.Domain.Entities;

namespace Doclyn.Application.Common.Interfaces;

public interface IAiStructuredDataExtractor
{
    Task<Dictionary<string, object?>> ExtractAsync(
        string text,
        DocumentClass documentClass,
        IReadOnlyCollection<DocumentClassIndexer> indexers,
        CancellationToken cancellationToken = default);
}
