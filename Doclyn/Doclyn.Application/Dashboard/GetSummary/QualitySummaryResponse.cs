namespace Doclyn.Application.Dashboard.GetSummary;

public sealed record QualitySummaryResponse(
    decimal AverageConfidence,
    int FieldsValidated,
    int FieldsNeedsReview,
    int FieldsRejected);
