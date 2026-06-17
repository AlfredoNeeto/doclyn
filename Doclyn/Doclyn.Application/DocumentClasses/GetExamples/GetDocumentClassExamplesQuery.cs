using MediatR;

namespace Doclyn.Application.DocumentClasses.GetExamples;

public sealed record GetDocumentClassExamplesQuery(Guid DocumentClassId) : IRequest<IReadOnlyList<DocumentClassExampleResponse>>;
