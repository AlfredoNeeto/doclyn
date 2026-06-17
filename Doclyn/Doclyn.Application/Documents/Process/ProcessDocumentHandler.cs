using Doclyn.Application.Common.Exceptions;
using Doclyn.Application.Common.Interfaces;
using Doclyn.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Doclyn.Application.Documents.Process;

public sealed class ProcessDocumentHandler : IRequestHandler<ProcessDocumentCommand, ProcessDocumentResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDocumentProcessingQueue _documentProcessingQueue;

    public ProcessDocumentHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDocumentProcessingQueue documentProcessingQueue)
    {
        _context = context;
        _currentUserService = currentUserService;
        _documentProcessingQueue = documentProcessingQueue;
    }

    public async Task<ProcessDocumentResponse> Handle(ProcessDocumentCommand request, CancellationToken cancellationToken)
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

        if (_currentUserService.Role != UserRole.Admin.ToString() && document.UserId != _currentUserService.UserId.Value)
        {
            throw new UnauthorizedAccessException("Access denied.");
        }

        if (document.DocumentStatus == DocumentStatus.Processing)
        {
            throw new InvalidOperationException("Document is already processing.");
        }

        if (document.DocumentStatus == DocumentStatus.Processed)
        {
            throw new InvalidOperationException("Document has already been processed.");
        }

        if (document.DocumentStatus is not DocumentStatus.Pending and not DocumentStatus.Failed)
        {
            throw new InvalidOperationException("Document cannot be processed in its current state.");
        }

        _documentProcessingQueue.Enqueue(document.Id);

        return new ProcessDocumentResponse(
            document.Id,
            DocumentStatus.Processing.ToString(),
            null,
            null);
    }
}
