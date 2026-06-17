using MediatR;

namespace Doclyn.Application.Documents.Process;

public sealed record ProcessDocumentCommand(Guid DocumentId) : IRequest<ProcessDocumentResponse>;
