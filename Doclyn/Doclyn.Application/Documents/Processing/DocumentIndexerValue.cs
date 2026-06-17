namespace Doclyn.Application.Documents.Processing;

public sealed record DocumentIndexerValue(
    object? Value,
    string Source,
    double Confidence);
