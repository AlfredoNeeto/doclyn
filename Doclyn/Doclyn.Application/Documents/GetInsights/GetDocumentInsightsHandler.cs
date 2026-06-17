using Doclyn.Application.Common.Interfaces;
using Doclyn.Application.Common.Exceptions;
using Doclyn.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Doclyn.Application.Documents.GetInsights;

public sealed class GetDocumentInsightsHandler : IRequestHandler<GetDocumentInsightsQuery, IReadOnlyList<DocumentInsightResponse>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetDocumentInsightsHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<IReadOnlyList<DocumentInsightResponse>> Handle(
        GetDocumentInsightsQuery request,
        CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue)
            throw new UnauthorizedAccessException("User is not authenticated.");

        var document = await _context.Documents
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == request.DocumentId, cancellationToken);

        if (document is null)
            throw new NotFoundException("Document not found.");

        if (_currentUserService.Role != UserRole.Admin.ToString()
            && document.UserId != _currentUserService.UserId.Value)
            throw new UnauthorizedAccessException("Access denied.");

        var insights = await _context.DocumentInsights
            .AsNoTracking()
            .Where(di => di.DocumentId == request.DocumentId)
            .OrderByDescending(di => di.Severity)
            .ThenByDescending(di => di.CreatedAt)
            .Select(di => new DocumentInsightResponse(
                di.Id,
                di.Type.ToString(),
                di.Severity.ToString(),
                di.Title,
                di.Message,
                di.Confidence,
                di.Source.ToString(),
                di.RelatedFieldName,
                di.CreatedAt))
            .ToListAsync(cancellationToken);

        return insights;
    }
}
