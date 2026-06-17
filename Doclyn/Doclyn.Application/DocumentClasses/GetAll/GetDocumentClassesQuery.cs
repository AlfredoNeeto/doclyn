using MediatR;

namespace Doclyn.Application.DocumentClasses.GetAll;

public sealed record GetDocumentClassesQuery : IRequest<GetDocumentClassesResponse>;
