using Doclyn.Domain.Enums;
using MediatR;

namespace Doclyn.Application.DocumentClassIndexers.Create;

public sealed record CreateDocumentClassIndexerCommand(
    Guid DocumentClassId,
    string Name,
    string DisplayName,
    string Description,
    IndexerDataType DataType,
    bool IsRequired,
    bool IsMultiple,
    string? ExtractionHint = null,
    string? RegexPattern = null) : IRequest<DocumentClassIndexerCreatedResponse>;

public sealed record DocumentClassIndexerCreatedResponse(Guid Id);
