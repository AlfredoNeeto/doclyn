using Doclyn.Application.Common.Exceptions;
using Doclyn.Application.Common.Interfaces;
using Doclyn.Domain.Entities;
using Doclyn.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Doclyn.Application.Documents.Delete;

public sealed class DeleteDocumentHandler : IRequestHandler<DeleteDocumentCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public DeleteDocumentHandler(
        IApplicationDbContext context,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task Handle(DeleteDocumentCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            throw new UnauthorizedAccessException("User is not authenticated.");
        }

        var document = await _context.Documents
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

        document.Delete(_currentUserService.UserId.Value);

        _context.ProcessingLogs.Add(ProcessingLog.Create(
            document.Id,
            "DocumentDeleted",
            "Document soft deleted successfully.",
            DocumentStatus.Success));

        await _unitOfWork.CommitAsync(cancellationToken);
    }
}
