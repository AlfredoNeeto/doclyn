namespace Doclyn.Infrastructure.Validation;

public sealed class FieldConfidenceOptions
{
    public const string Section = "FieldConfidence";

    public decimal ValidatedThreshold { get; init; } = 0.90m;
    public decimal ReviewThreshold { get; init; } = 0.70m;
    public decimal DefaultAiConfidence { get; init; } = 0.80m;
}
