using Doclyn.Domain.Enums;
using MediatR;

namespace Doclyn.Application.DocumentClassIndexers.Update;

public sealed record UpdateDocumentClassIndexerCommand(
    Guid DocumentClassId,
    Guid Id,
    string Name,
    string DisplayName,
    string Description,
    IndexerDataType DataType,
    bool IsRequired,
    bool IsMultiple,
    string? ExtractionHint = null,
    string? RegexPattern = null) : IRequest;
