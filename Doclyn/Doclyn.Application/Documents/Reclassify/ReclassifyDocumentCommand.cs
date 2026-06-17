using MediatR;

namespace Doclyn.Application.Documents.Reclassify;

public sealed record ReclassifyDocumentCommand(Guid DocumentId) : IRequest<ReclassifyDocumentResponse>;
