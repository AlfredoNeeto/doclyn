using Doclyn.Application.Common.Interfaces;
using Doclyn.Application.Common.Exceptions;
using Doclyn.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Doclyn.Application.Documents.GetById;

public sealed class GetDocumentByIdHandler : IRequestHandler<GetDocumentByIdQuery, GetDocumentByIdResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetDocumentByIdHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<GetDocumentByIdResponse> Handle(
        GetDocumentByIdQuery request,
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

        return new GetDocumentByIdResponse(
            document.Id,
            document.UserId,
            document.FileName,
            document.FileHash,
            document.DocumentType,
            document.DocumentStatus.ToString(),
            document.CreatedAt,
            document.UpdatedAt,
            document.ProcessedAt);
    }
}
