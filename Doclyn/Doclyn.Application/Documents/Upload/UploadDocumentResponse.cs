namespace Doclyn.Application.Documents.Upload;

public sealed record UploadDocumentResponse(
    Guid Id,
    string FileName,
    string FileHash,
    string DocumentType,
    string DocumentStatus,
    DateTime CreatedAt);
