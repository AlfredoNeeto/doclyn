using MediatR;

namespace Doclyn.Application.DocumentClassIndexers.GetByDocumentClass;

public sealed record GetDocumentClassIndexersQuery(Guid DocumentClassId) : IRequest<IReadOnlyCollection<DocumentClassIndexerResponse>>;
