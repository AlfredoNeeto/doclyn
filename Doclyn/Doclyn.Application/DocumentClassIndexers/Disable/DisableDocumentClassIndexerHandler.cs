using Doclyn.Application.Common.Interfaces;
using MediatR;

namespace Doclyn.Application.DocumentClassIndexers.Disable;

public sealed class DisableDocumentClassIndexerHandler : IRequestHandler<DisableDocumentClassIndexerCommand>
{
    private readonly IDocumentClassIndexerCatalogService _catalogService;

    public DisableDocumentClassIndexerHandler(IDocumentClassIndexerCatalogService catalogService)
    {
        _catalogService = catalogService;
    }

    public async Task Handle(
        DisableDocumentClassIndexerCommand request,
        CancellationToken cancellationToken)
    {
        await _catalogService.DisableAsync(
            request.DocumentClassId,
            request.Id,
            cancellationToken);
    }
}
