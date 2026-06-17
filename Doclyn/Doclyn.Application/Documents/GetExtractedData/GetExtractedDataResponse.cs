namespace Doclyn.Application.Documents.GetExtractedData;

public sealed record GetExtractedDataResponse(
    Guid DocumentId,
    object? Data,
    DateTime? CreatedAt);
