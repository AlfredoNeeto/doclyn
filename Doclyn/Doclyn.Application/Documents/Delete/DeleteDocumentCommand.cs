using MediatR;

namespace Doclyn.Application.Documents.Delete;

public sealed record DeleteDocumentCommand(Guid DocumentId) : IRequest;
