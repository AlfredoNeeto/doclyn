using Doclyn.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Doclyn.Application.DocumentClasses.GetById;

public sealed class GetDocumentClassByIdHandler : IRequestHandler<GetDocumentClassByIdQuery, GetDocumentClassByIdResponse>
{
    private readonly IApplicationDbContext _context;

    public GetDocumentClassByIdHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<GetDocumentClassByIdResponse> Handle(
        GetDocumentClassByIdQuery request,
        CancellationToken cancellationToken)
    {
        var documentClass = await _context.DocumentClasses
            .AsNoTracking()
            .FirstOrDefaultAsync(dc => dc.Id == request.DocumentClassId, cancellationToken);

        if (documentClass is null)
        {
            throw new InvalidOperationException("Document class not found.");
        }

        return new GetDocumentClassByIdResponse(
            documentClass.Id,
            documentClass.Name,
            documentClass.DisplayName,
            documentClass.Group,
            documentClass.SubGroup,
            documentClass.Description,
            documentClass.IsSystemDefined,
            documentClass.IsActive,
            documentClass.CreatedAt,
            documentClass.UpdatedAt);
    }
}
