using MediatR;

namespace Doclyn.Application.Documents.GenerateInsights;

public sealed record GenerateDocumentInsightsCommand(Guid DocumentId) : IRequest<GenerateDocumentInsightsResponse>;
