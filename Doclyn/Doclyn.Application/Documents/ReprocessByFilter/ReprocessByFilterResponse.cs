namespace Doclyn.Application.Documents.ReprocessByFilter;

public sealed record ReprocessByFilterResponse(
    int Matched,
    int Enqueued,
    int Skipped);
