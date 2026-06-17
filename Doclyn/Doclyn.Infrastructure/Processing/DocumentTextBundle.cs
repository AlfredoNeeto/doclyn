namespace Doclyn.Infrastructure.Processing;

public sealed record DocumentTextBundle(
    string NativeText,
    string? OcrText,
    string MergedText,
    bool OcrUsed);
