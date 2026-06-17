namespace Doclyn.Application.Documents.Processing;

public sealed record SemanticClassificationResult(
    Guid? DocumentClassId,
    string DocumentType,
    string Group,
    string SubGroup,
    decimal Confidence,
    bool ReusedExistingClass,
    bool NewClassSuggested);
