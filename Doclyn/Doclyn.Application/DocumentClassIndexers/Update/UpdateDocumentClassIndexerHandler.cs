using Doclyn.Application.Common.Interfaces;
using MediatR;

namespace Doclyn.Application.DocumentClassIndexers.Update;

public sealed class UpdateDocumentClassIndexerHandler : IRequestHandler<UpdateDocumentClassIndexerCommand>
{
    private readonly IDocumentClassIndexerCatalogService _catalogService;

    public UpdateDocumentClassIndexerHandler(IDocumentClassIndexerCatalogService catalogService)
    {
        _catalogService = catalogService;
    }

    public async Task Handle(
        UpdateDocumentClassIndexerCommand request,
        CancellationToken cancellationToken)
    {
        await _catalogService.UpdateAsync(
            request.DocumentClassId,
            request.Id,
            request.Name,
            request.DisplayName,
            request.Description,
            request.DataType,
            request.IsRequired,
            request.IsMultiple,
            request.ExtractionHint,
            request.RegexPattern,
            cancellationToken);
    }
}
