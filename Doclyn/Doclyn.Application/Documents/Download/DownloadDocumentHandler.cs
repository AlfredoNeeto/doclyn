using Doclyn.Application.Common.Exceptions;
using Doclyn.Application.Common.Interfaces;
using Doclyn.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Doclyn.Application.Documents.Download;

public sealed class DownloadDocumentHandler : IRequestHandler<DownloadDocumentQuery, DownloadDocumentResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IFileStorageService _fileStorageService;

    public DownloadDocumentHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IFileStorageService fileStorageService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _fileStorageService = fileStorageService;
    }

    public async Task<DownloadDocumentResponse> Handle(
        DownloadDocumentQuery request,
        CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue)
            throw new UnauthorizedAccessException("User is not authenticated.");

        var document = await _context.Documents
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == request.DocumentId, cancellationToken);

        if (document is null)
            throw new NotFoundException("Document not found.");

        if (_currentUserService.Role != UserRole.Admin.ToString()
            && document.UserId != _currentUserService.UserId.Value)
            throw new UnauthorizedAccessException("Access denied.");

        var fileStream = await _fileStorageService.DownloadAsync(
            document.StoragePath,
            cancellationToken);

        return new DownloadDocumentResponse(
            fileStream,
            document.FileName,
            "application/pdf");
    }
}
