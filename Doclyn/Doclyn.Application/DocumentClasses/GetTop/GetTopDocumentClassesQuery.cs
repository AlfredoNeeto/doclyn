using MediatR;

namespace Doclyn.Application.DocumentClasses.GetTop;

public sealed record GetTopDocumentClassesQuery(int Take = 10) : IRequest<IReadOnlyList<GetTopDocumentClassesResponse>>;
