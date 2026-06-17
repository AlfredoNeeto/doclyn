using Doclyn.Application.Common.Interfaces;
using MediatR;

namespace Doclyn.Application.DocumentClassIndexers.GetByDocumentClass;

public sealed class GetDocumentClassIndexersHandler : IRequestHandler<GetDocumentClassIndexersQuery, IReadOnlyCollection<DocumentClassIndexerResponse>>
{
    private readonly IDocumentClassIndexerCatalogService _catalogService;

    public GetDocumentClassIndexersHandler(IDocumentClassIndexerCatalogService catalogService)
    {
        _catalogService = catalogService;
    }

    public async Task<IReadOnlyCollection<DocumentClassIndexerResponse>> Handle(
        GetDocumentClassIndexersQuery request,
        CancellationToken cancellationToken)
    {
        var indexers = await _catalogService.GetActiveByDocumentClassAsync(
            request.DocumentClassId,
            cancellationToken);

        return indexers
            .Select(dci => new DocumentClassIndexerResponse(
                dci.Id,
                dci.Name,
                dci.DisplayName,
                dci.Description,
                dci.DataType,
                dci.IsRequired,
                dci.IsMultiple,
                dci.ExtractionHint,
                !string.IsNullOrWhiteSpace(dci.RegexPattern),
                dci.IsActive,
                dci.CreatedAt,
                dci.UpdatedAt))
            .ToList();
    }
}
