namespace Doclyn.Application.Documents.GetById;

public sealed record GetDocumentByIdResponse(
    Guid Id,
    Guid UserId,
    string FileName,
    string FileHash,
    string DocumentType,
    string DocumentStatus,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    DateTime? ProcessedAt);
