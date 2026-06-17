using Doclyn.Application.Common.Interfaces;
using Doclyn.Application.Documents.Reprocessing;
using Doclyn.Domain.Entities;
using Doclyn.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Doclyn.Application.Documents.ReprocessBatch;

public sealed class ReprocessBatchHandler : IRequestHandler<ReprocessBatchCommand, ReprocessBatchResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDocumentProcessingQueue _documentProcessingQueue;

    public ReprocessBatchHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDocumentProcessingQueue documentProcessingQueue)
    {
        _context = context;
        _currentUserService = currentUserService;
        _documentProcessingQueue = documentProcessingQueue;
    }

    public async Task<ReprocessBatchResponse> Handle(ReprocessBatchCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = DocumentReprocessingGuard.EnsureAuthenticated(_currentUserService);
        var documentIds = request.DocumentIds
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToArray();

        if (documentIds.Length == 0)
        {
            return new ReprocessBatchResponse(0, 0, 0);
        }

        var documents = await _context.Documents
            .Where(d => documentIds.Contains(d.Id))
            .ToListAsync(cancellationToken);

        var enqueued = 0;
        var skipped = 0;

        foreach (var documentId in documentIds)
        {
            var document = documents.FirstOrDefault(d => d.Id == documentId);

            if (document is null)
            {
                skipped++;
                continue;
            }

            if (_currentUserService.Role != UserRole.Admin.ToString() && document.UserId != currentUserId)
            {
                skipped++;
                continue;
            }

            if (!DocumentReprocessingGuard.CanEnqueue(document))
            {
                _context.ProcessingLogs.Add(ProcessingLog.Create(
                    document.Id,
                    "BatchReprocessFailed",
                    "Document skipped because it is already processing.",
                    DocumentStatus.Failed));
                skipped++;
                continue;
            }

            _context.ProcessingLogs.Add(ProcessingLog.Create(
                document.Id,
                "BatchReprocessStarted",
                "Document selected for batch reprocessing.",
                DocumentStatus.Success));

            _documentProcessingQueue.Enqueue(document.Id);

            _context.ProcessingLogs.Add(ProcessingLog.Create(
                document.Id,
                "BatchReprocessCompleted",
                "Document enqueued for batch reprocessing.",
                DocumentStatus.Success));

            enqueued++;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return new ReprocessBatchResponse(documentIds.Length, enqueued, skipped);
    }
}
