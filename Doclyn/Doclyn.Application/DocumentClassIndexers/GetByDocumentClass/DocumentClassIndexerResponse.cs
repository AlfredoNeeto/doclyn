using Doclyn.Domain.Enums;

namespace Doclyn.Application.DocumentClassIndexers.GetByDocumentClass;

public sealed record DocumentClassIndexerResponse(
    Guid Id,
    string Name,
    string DisplayName,
    string Description,
    IndexerDataType DataType,
    bool IsRequired,
    bool IsMultiple,
    string? ExtractionHint,
    bool HasRegexPattern,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
