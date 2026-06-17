using Doclyn.Application.Common.Exceptions;
using Doclyn.Application.Common.Interfaces;
using Doclyn.Domain.Entities;
using Doclyn.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Doclyn.Application.Documents.Restore;

public sealed class RestoreDocumentHandler : IRequestHandler<RestoreDocumentCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public RestoreDocumentHandler(
        IApplicationDbContext context,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task Handle(RestoreDocumentCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            throw new UnauthorizedAccessException("User is not authenticated.");
        }

        if (_currentUserService.Role != UserRole.Admin.ToString())
        {
            throw new UnauthorizedAccessException("Access denied.");
        }

        var document = await _context.Documents
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(d => d.Id == request.DocumentId, cancellationToken);

        if (document is null)
        {
            throw new NotFoundException("Document not found.");
        }

        document.Restore();

        _context.ProcessingLogs.Add(ProcessingLog.Create(
            document.Id,
            "DocumentRestored",
            "Document restored successfully.",
            DocumentStatus.Success));

        await _unitOfWork.CommitAsync(cancellationToken);
    }
}
