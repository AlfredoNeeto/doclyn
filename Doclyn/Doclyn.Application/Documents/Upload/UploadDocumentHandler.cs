using Doclyn.Application.Common.Interfaces;
using Doclyn.Domain.Constants;
using Doclyn.Domain.Entities;
using Doclyn.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Doclyn.Application.Documents.Upload;

public sealed class UploadDocumentHandler : IRequestHandler<UploadDocumentCommand, UploadDocumentResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileStorageService _fileStorageService;
    private readonly IFileHashService _fileHashService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDocumentProcessingQueue _documentProcessingQueue;
    private readonly ILogger<UploadDocumentHandler> _logger;

    public UploadDocumentHandler(
        IApplicationDbContext context,
        IUnitOfWork unitOfWork,
        IFileStorageService fileStorageService,
        IFileHashService fileHashService,
        ICurrentUserService currentUserService,
        IDocumentProcessingQueue documentProcessingQueue,
        ILogger<UploadDocumentHandler> logger)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _fileStorageService = fileStorageService;
        _fileHashService = fileHashService;
        _currentUserService = currentUserService;
        _documentProcessingQueue = documentProcessingQueue;
        _logger = logger;
    }

    public async Task<UploadDocumentResponse> Handle(
        UploadDocumentCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting document upload for file {FileName}", request.FileName);

        if (!_currentUserService.UserId.HasValue)
        {
            _logger.LogWarning("Upload attempt without authenticated user");
            throw new UnauthorizedAccessException("User is not authenticated.");
        }

        var userId = _currentUserService.UserId.Value;
        var originalFileName = request.FileName.Trim();
        var documentId = Guid.NewGuid();
        var storagePath = $"documents/{userId}/{documentId}/original.pdf";

        _logger.LogInformation(
            "File {FileName} validated for user {UserId}",
            originalFileName,
            userId);

        var fileHash = await _fileHashService.ComputeSha256Async(request.FileStream, cancellationToken);

        _logger.LogInformation(
            "SHA-256 calculated for document {DocumentId}",
            documentId);

        try
        {
            if (request.FileStream.CanSeek)
            {
                request.FileStream.Position = 0;
            }

            _logger.LogInformation(
                "Sending document {DocumentId} to object storage",
                documentId);

            await _fileStorageService.UploadAsync(
                request.FileStream,
                storagePath,
                request.ContentType,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store document {DocumentId} in object storage", documentId);
            throw;
        }

        var document = Document.Create(
            userId,
            originalFileName,
            fileHash,
            storagePath,
            DocumentTypes.Unknown,
            documentId);

        var log = ProcessingLog.Create(
            document.Id,
            "Upload",
            "Document uploaded and stored successfully.",
            DocumentStatus.Success);

        try
        {
            _context.Documents.Add(document);
            _context.ProcessingLogs.Add(log);

            await _unitOfWork.CommitAsync(cancellationToken);

            _documentProcessingQueue.Enqueue(document.Id);

            _logger.LogInformation(
                "Document {DocumentId} registered in database and enqueued for processing",
                document.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register document {DocumentId} in database", document.Id);
            throw;
        }

        return new UploadDocumentResponse(
            document.Id,
            document.FileName,
            document.FileHash,
            document.DocumentType,
            document.DocumentStatus.ToString(),
            document.CreatedAt);
    }
}
