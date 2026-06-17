using MediatR;

namespace Doclyn.Application.Documents.GetInsights;

public sealed record GetDocumentInsightsQuery(Guid DocumentId) : IRequest<IReadOnlyList<DocumentInsightResponse>>;
