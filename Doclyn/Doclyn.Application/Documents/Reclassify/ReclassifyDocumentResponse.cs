namespace Doclyn.Application.Documents.Reclassify;

public sealed record ReclassifyDocumentResponse(
    Guid DocumentId,
    string Status);
