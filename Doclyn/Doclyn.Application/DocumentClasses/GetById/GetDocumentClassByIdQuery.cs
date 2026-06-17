using MediatR;

namespace Doclyn.Application.DocumentClasses.GetById;

public sealed record GetDocumentClassByIdQuery(Guid DocumentClassId) : IRequest<GetDocumentClassByIdResponse>;
