using Doclyn.Domain.Entities;
using Doclyn.Domain.Enums;

namespace Doclyn.Application.Common.Interfaces;

public interface IDocumentClassIndexerCatalogService
{
    Task<IReadOnlyCollection<DocumentClassIndexer>> GetActiveByDocumentClassAsync(
        Guid documentClassId,
        CancellationToken cancellationToken = default);

    Task<DocumentClassIndexer> CreateAsync(
        Guid documentClassId,
        string name,
        string displayName,
        string description,
        IndexerDataType dataType,
        bool isRequired,
        bool isMultiple,
        string? extractionHint = null,
        string? regexPattern = null,
        CancellationToken cancellationToken = default);

    Task<DocumentClassIndexer> UpdateAsync(
        Guid documentClassId,
        Guid id,
        string name,
        string displayName,
        string description,
        IndexerDataType dataType,
        bool isRequired,
        bool isMultiple,
        string? extractionHint = null,
        string? regexPattern = null,
        CancellationToken cancellationToken = default);

    Task DisableAsync(
        Guid documentClassId,
        Guid id,
        CancellationToken cancellationToken = default);
}
