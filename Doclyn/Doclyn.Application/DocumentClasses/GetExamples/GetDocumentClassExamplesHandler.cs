using Doclyn.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Doclyn.Application.DocumentClasses.GetExamples;

public sealed class GetDocumentClassExamplesHandler : IRequestHandler<GetDocumentClassExamplesQuery, IReadOnlyList<DocumentClassExampleResponse>>
{
    private readonly IApplicationDbContext _context;

    public GetDocumentClassExamplesHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<DocumentClassExampleResponse>> Handle(
        GetDocumentClassExamplesQuery request,
        CancellationToken cancellationToken)
    {
        var examples = await _context.DocumentClassExamples
            .AsNoTracking()
            .Where(dce => dce.DocumentClassId == request.DocumentClassId)
            .OrderByDescending(dce => dce.CreatedAt)
            .Select(dce => new DocumentClassExampleResponse(
                dce.Id,
                dce.DocumentId,
                dce.Document.FileName,
                dce.Confidence,
                dce.CreatedAt))
            .ToListAsync(cancellationToken);

        return examples;
    }
}
