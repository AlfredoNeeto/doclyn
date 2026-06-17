using Doclyn.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Doclyn.Application.DocumentClasses.GetAll;

public sealed class GetDocumentClassesHandler : IRequestHandler<GetDocumentClassesQuery, GetDocumentClassesResponse>
{
    private readonly IApplicationDbContext _context;

    public GetDocumentClassesHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<GetDocumentClassesResponse> Handle(
        GetDocumentClassesQuery request,
        CancellationToken cancellationToken)
    {
        var items = await _context.DocumentClasses
            .AsNoTracking()
            .OrderBy(dc => dc.Name)
            .Select(dc => new DocumentClassListItemResponse(
                dc.Id,
                dc.Name,
                dc.DisplayName,
                dc.Group,
                dc.SubGroup,
                dc.IsActive))
            .ToListAsync(cancellationToken);

        return new GetDocumentClassesResponse(items);
    }
}
