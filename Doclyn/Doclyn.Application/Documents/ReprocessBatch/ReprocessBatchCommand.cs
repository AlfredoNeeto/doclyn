using MediatR;

namespace Doclyn.Application.Documents.ReprocessBatch;

public sealed record ReprocessBatchCommand(IReadOnlyCollection<Guid> DocumentIds) : IRequest<ReprocessBatchResponse>;
