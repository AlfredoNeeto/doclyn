using Doclyn.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Doclyn.Application.DocumentClasses.GetTop;

public sealed class GetTopDocumentClassesHandler : IRequestHandler<GetTopDocumentClassesQuery, IReadOnlyList<GetTopDocumentClassesResponse>>
{
    private readonly IApplicationDbContext _context;

    public GetTopDocumentClassesHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<GetTopDocumentClassesResponse>> Handle(
        GetTopDocumentClassesQuery request,
        CancellationToken cancellationToken)
    {
        var classes = await _context.DocumentClasses
            .AsNoTracking()
            .Where(dc => dc.IsActive)
            .ToListAsync(cancellationToken);

        var exampleCounts = await _context.DocumentClassExamples
            .AsNoTracking()
            .GroupBy(dce => dce.DocumentClassId)
            .Select(g => new { DocumentClassId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.DocumentClassId, g => g.Count, cancellationToken);

        return classes
            .Select(dc => new GetTopDocumentClassesResponse(
                dc.Id,
                dc.Name,
                dc.DisplayName,
                dc.Group,
                dc.SubGroup,
                exampleCounts.TryGetValue(dc.Id, out var count) ? count : 0))
            .OrderByDescending(r => r.ExampleCount)
            .Take(request.Take)
            .ToList();
    }
}
