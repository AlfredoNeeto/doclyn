namespace Doclyn.Infrastructure.Classification;

public sealed class ClassificationOptions
{
    public const string Section = "Classification";

    public decimal ReuseThreshold { get; init; } = 0.85m;
}
