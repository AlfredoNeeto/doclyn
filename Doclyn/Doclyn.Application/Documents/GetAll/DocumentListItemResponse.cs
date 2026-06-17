namespace Doclyn.Application.Documents.GetAll;

public sealed record DocumentListItemResponse(
    Guid Id,
    string FileName,
    string DocumentType,
    string DocumentStatus,
    DateTime CreatedAt,
    DateTime? ProcessedAt);
