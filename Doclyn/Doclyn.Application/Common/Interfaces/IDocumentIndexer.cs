using Doclyn.Application.Documents.Processing;
using Doclyn.Domain.Entities;

namespace Doclyn.Application.Common.Interfaces;

public interface IDocumentIndexer
{
    Dictionary<string, DocumentIndexerValue> ExtractIndexes(
        string text,
        IReadOnlyCollection<DocumentClassIndexer> indexers);
}
