namespace Doclyn.Infrastructure.AI;

public sealed record RawSemanticClassificationResult(
    string DocumentClassName,
    string Group,
    string SubGroup,
    bool ReuseExistingClass,
    decimal Confidence);
