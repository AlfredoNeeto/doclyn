using MediatR;

namespace Doclyn.Application.Documents.GetById;

public sealed record GetDocumentByIdQuery(Guid DocumentId) : IRequest<GetDocumentByIdResponse>;
