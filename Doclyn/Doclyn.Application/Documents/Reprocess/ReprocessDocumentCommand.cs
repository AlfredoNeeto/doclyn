using MediatR;

namespace Doclyn.Application.Documents.Reprocess;

public sealed record ReprocessDocumentCommand(Guid DocumentId) : IRequest<ReprocessDocumentResponse>;
