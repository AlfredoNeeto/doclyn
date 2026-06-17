namespace Doclyn.Application.Documents.ReprocessBatch;

public sealed record ReprocessBatchResponse(
    int Requested,
    int Enqueued,
    int Skipped);
