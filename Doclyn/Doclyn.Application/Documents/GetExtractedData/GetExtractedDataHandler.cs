using Doclyn.Application.Common.Interfaces;
using Doclyn.Application.Common.Exceptions;
using Doclyn.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
namespace Doclyn.Application.Documents.GetExtractedData;

public sealed class GetExtractedDataHandler : IRequestHandler<GetExtractedDataQuery, GetExtractedDataResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetExtractedDataHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<GetExtractedDataResponse> Handle(
        GetExtractedDataQuery request,
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

        var extractedData = await _context.ExtractedData
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.DocumentId == request.DocumentId, cancellationToken);

        if (extractedData is null)
        {
            return new GetExtractedDataResponse(request.DocumentId, null, null);
        }

        return new GetExtractedDataResponse(
            extractedData.DocumentId,
            extractedData.Data.RootElement.Clone(),
            extractedData.CreatedAt);
    }
}
