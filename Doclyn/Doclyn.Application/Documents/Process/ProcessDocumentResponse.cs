namespace Doclyn.Application.Documents.Process;

public sealed record ProcessDocumentResponse(
    Guid DocumentId,
    string Status,
    string? DocumentType,
    DateTime? ProcessedAt);
