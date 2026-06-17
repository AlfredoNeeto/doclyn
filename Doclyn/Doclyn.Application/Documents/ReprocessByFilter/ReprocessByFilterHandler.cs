using Doclyn.Application.Common.Interfaces;
using Doclyn.Application.Documents.Reprocessing;
using Doclyn.Domain.Entities;
using Doclyn.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Doclyn.Application.Documents.ReprocessByFilter;

public sealed class ReprocessByFilterHandler : IRequestHandler<ReprocessByFilterCommand, ReprocessByFilterResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDocumentProcessingQueue _documentProcessingQueue;

    public ReprocessByFilterHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDocumentProcessingQueue documentProcessingQueue)
    {
        _context = context;
        _currentUserService = currentUserService;
        _documentProcessingQueue = documentProcessingQueue;
    }

    public async Task<ReprocessByFilterResponse> Handle(ReprocessByFilterCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = DocumentReprocessingGuard.EnsureAuthenticated(_currentUserService);

        IQueryable<Document> query = _context.Documents;

        if (_currentUserService.Role != UserRole.Admin.ToString())
        {
            query = query.Where(d => d.UserId == currentUserId);
        }

        if (!string.IsNullOrWhiteSpace(request.Status)
            && Enum.TryParse<DocumentStatus>(request.Status, true, out var status))
        {
            query = query.Where(d => d.DocumentStatus == status);
        }

        if (!string.IsNullOrWhiteSpace(request.DocumentType))
        {
            query = query.Where(d => d.DocumentType == request.DocumentType.Trim());
        }

        if (request.From.HasValue)
        {
            query = query.Where(d => d.CreatedAt >= request.From.Value);
        }

        if (request.To.HasValue)
        {
            query = query.Where(d => d.CreatedAt <= request.To.Value);
        }

        var documents = await query.ToListAsync(cancellationToken);
        var matched = documents.Count;
        var enqueued = 0;
        var skipped = 0;

        foreach (var document in documents)
        {
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
                "Document matched filter-based batch reprocessing.",
                DocumentStatus.Success));

            _documentProcessingQueue.Enqueue(document.Id);

            _context.ProcessingLogs.Add(ProcessingLog.Create(
                document.Id,
                "BatchReprocessCompleted",
                "Document enqueued by filter-based batch reprocessing.",
                DocumentStatus.Success));

            enqueued++;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return new ReprocessByFilterResponse(matched, enqueued, skipped);
    }
}
