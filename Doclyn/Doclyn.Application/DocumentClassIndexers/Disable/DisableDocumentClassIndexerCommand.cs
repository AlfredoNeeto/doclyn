using MediatR;

namespace Doclyn.Application.DocumentClassIndexers.Disable;

public sealed record DisableDocumentClassIndexerCommand(
    Guid DocumentClassId,
    Guid Id) : IRequest;
