using MediatR;

namespace Doclyn.Application.Documents.GetLogs;

public sealed record GetDocumentLogsQuery(Guid DocumentId) : IRequest<IReadOnlyList<GetDocumentLogResponse>>;
