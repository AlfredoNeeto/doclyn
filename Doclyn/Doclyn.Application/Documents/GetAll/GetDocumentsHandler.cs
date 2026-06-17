using Doclyn.Application.Common.Interfaces;
using Doclyn.Domain.Entities;
using Doclyn.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Doclyn.Application.Documents.GetAll;

public sealed class GetDocumentsHandler : IRequestHandler<GetDocumentsQuery, GetDocumentsResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetDocumentsHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<GetDocumentsResponse> Handle(
        GetDocumentsQuery request,
        CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            throw new UnauthorizedAccessException("User is not authenticated.");
        }

        var query = _context.Documents.AsNoTracking();

        if (_currentUserService.Role != UserRole.Admin.ToString())
        {
            query = query.Where(d => d.UserId == _currentUserService.UserId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Status) &&
            Enum.TryParse<DocumentStatus>(request.Status, true, out var status))
        {
            query = query.Where(d => d.DocumentStatus == status);
        }

        if (!string.IsNullOrWhiteSpace(request.DocumentType))
        {
            var documentType = request.DocumentType.Trim();
            query = query.Where(d => d.DocumentType == documentType);
        }

        if (request.From.HasValue)
        {
            query = query.Where(d => d.CreatedAt >= request.From.Value);
        }

        if (request.To.HasValue)
        {
            query = query.Where(d => d.CreatedAt <= request.To.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLowerInvariant();
            query = query.Where(d => EF.Functions.Like(d.FileName.ToLower(), $"%{search}%"));
        }

        var totalItems = await query.CountAsync(cancellationToken);

        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 10 : request.PageSize;
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        var items = await query
            .OrderByDescending(d => d.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(d => new DocumentListItemResponse(
                d.Id,
                d.FileName,
                d.DocumentType,
                d.DocumentStatus.ToString(),
                d.CreatedAt,
                d.ProcessedAt))
            .ToListAsync(cancellationToken);

        return new GetDocumentsResponse(
            page,
            pageSize,
            totalItems,
            totalPages,
            items);
    }
}
