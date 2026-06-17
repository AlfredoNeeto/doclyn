using Doclyn.Application.Common.Interfaces;
using Doclyn.Application.Common.Exceptions;
using Doclyn.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Doclyn.Application.Documents.GetLogs;

public sealed class GetDocumentLogsHandler : IRequestHandler<GetDocumentLogsQuery, IReadOnlyList<GetDocumentLogResponse>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetDocumentLogsHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<IReadOnlyList<GetDocumentLogResponse>> Handle(
        GetDocumentLogsQuery request,
        CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            throw new UnauthorizedAccessException("User is not authenticated.");
        }

        var document = await _context.Documents
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == request.DocumentId, cancellationToken);

        if (document is null)
        {
            throw new NotFoundException("Document not found.");
        }

        if (_currentUserService.Role != UserRole.Admin.ToString() &&
            document.UserId != _currentUserService.UserId.Value)
        {
            throw new UnauthorizedAccessException("Access denied.");
        }

        var logs = await _context.ProcessingLogs
            .AsNoTracking()
            .Where(l => l.DocumentId == request.DocumentId)
            .OrderByDescending(l => l.CreatedAt)
            .Select(l => new GetDocumentLogResponse(
                l.Id,
                l.Step,
                l.Status.ToString(),
                l.Message,
                l.CreatedAt))
            .ToListAsync(cancellationToken);

        return logs;
    }
}
