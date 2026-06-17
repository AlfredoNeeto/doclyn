using Doclyn.Application.Common.Interfaces;
using Doclyn.Application.Documents.Reprocessing;
using Doclyn.Domain.Entities;
using Doclyn.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Doclyn.Application.Documents.Reprocess;

public sealed class ReprocessDocumentHandler : IRequestHandler<ReprocessDocumentCommand, ReprocessDocumentResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDocumentProcessingQueue _documentProcessingQueue;

    public ReprocessDocumentHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDocumentProcessingQueue documentProcessingQueue)
    {
        _context = context;
        _currentUserService = currentUserService;
        _documentProcessingQueue = documentProcessingQueue;
    }

    public async Task<ReprocessDocumentResponse> Handle(ReprocessDocumentCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = DocumentReprocessingGuard.EnsureAuthenticated(_currentUserService);

        var document = await _context.Documents
            .FirstOrDefaultAsync(d => d.Id == request.DocumentId, cancellationToken);

        DocumentReprocessingGuard.EnsureDocumentExists(document);
        DocumentReprocessingGuard.EnsureCanAccess(document!, _currentUserService, currentUserId);

        if (!DocumentReprocessingGuard.CanEnqueue(document!))
        {
            throw new InvalidOperationException("Document is already processing.");
        }

        _context.ProcessingLogs.Add(ProcessingLog.Create(
            document!.Id,
            "ReprocessRequested",
            "Document reprocessing requested.",
            DocumentStatus.Success));

        await _context.SaveChangesAsync(cancellationToken);

        _documentProcessingQueue.Enqueue(document.Id);

        return new ReprocessDocumentResponse(document.Id, DocumentStatus.Processing.ToString());
    }
}
