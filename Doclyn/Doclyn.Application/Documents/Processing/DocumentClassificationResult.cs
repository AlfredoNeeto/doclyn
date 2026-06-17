namespace Doclyn.Application.Documents.Processing;

public sealed record DocumentClassificationResult(
    string DocumentType,
    string Group,
    string Subgroup,
    double? Confidence = null);
