namespace Doclyn.Application.Documents.GetReviewFields;

public sealed record ReviewFieldItem(
    string FieldName,
    object? Value,
    decimal Confidence,
    string Source,
    string ValidationStatus);

public sealed record GetReviewFieldsResponse(
    Guid DocumentId,
    IReadOnlyList<ReviewFieldItem> Fields);
