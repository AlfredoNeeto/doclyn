using Doclyn.Application.Common.Interfaces;
using MediatR;

namespace Doclyn.Application.DocumentClassIndexers.Create;

public sealed class CreateDocumentClassIndexerHandler : IRequestHandler<CreateDocumentClassIndexerCommand, DocumentClassIndexerCreatedResponse>
{
    private readonly IDocumentClassIndexerCatalogService _catalogService;

    public CreateDocumentClassIndexerHandler(IDocumentClassIndexerCatalogService catalogService)
    {
        _catalogService = catalogService;
    }

    public async Task<DocumentClassIndexerCreatedResponse> Handle(
        CreateDocumentClassIndexerCommand request,
        CancellationToken cancellationToken)
    {
        var indexer = await _catalogService.CreateAsync(
            request.DocumentClassId,
            request.Name,
            request.DisplayName,
            request.Description,
            request.DataType,
            request.IsRequired,
            request.IsMultiple,
            request.ExtractionHint,
            request.RegexPattern,
            cancellationToken);

        return new DocumentClassIndexerCreatedResponse(indexer.Id);
    }
}
