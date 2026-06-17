using Doclyn.Domain.Entities;

namespace Doclyn.Application.Common.Interfaces;

public interface IDocumentClassCatalogService
{
    Task<DocumentClass?> FindByNameAsync(
        string name,
        CancellationToken cancellationToken = default);

    Task<DocumentClass> GetOrCreateAsync(
        string name,
        string group,
        string subGroup,
        string description,
        CancellationToken cancellationToken = default);

    Task RegisterExampleAsync(
        Guid documentClassId,
        Guid documentId,
        decimal confidence,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DocumentClass>> GetActiveAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DocumentClass>> GetTopUsedAsync(
        int take,
        CancellationToken cancellationToken = default);
}
