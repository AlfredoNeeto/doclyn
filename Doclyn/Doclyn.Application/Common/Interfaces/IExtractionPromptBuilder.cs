using Doclyn.Domain.Entities;

namespace Doclyn.Application.Common.Interfaces;

public interface IExtractionPromptBuilder
{
    string Build(
        DocumentClass documentClass,
        IReadOnlyCollection<DocumentClassIndexer> indexers,
        string documentText);
}
