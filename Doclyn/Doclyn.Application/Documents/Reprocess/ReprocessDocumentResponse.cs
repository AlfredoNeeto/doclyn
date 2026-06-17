namespace Doclyn.Application.Documents.Reprocess;

public sealed record ReprocessDocumentResponse(
    Guid DocumentId,
    string Status);
