using Doclyn.Infrastructure.Processing;

namespace Doclyn.Infrastructure.OCR;

public sealed class DocumentTextMergeService
{
    public DocumentTextBundle Merge(string nativeText, string? ocrText, bool ocrUsed)
    {
        nativeText ??= string.Empty;
        ocrText ??= string.Empty;

        var merged = (nativeText, ocrText) switch
        {
            (var n, var o) when string.IsNullOrWhiteSpace(n) && string.IsNullOrWhiteSpace(o) => string.Empty,
            (var n, var o) when string.IsNullOrWhiteSpace(o) => n.Trim(),
            (var n, var o) when string.IsNullOrWhiteSpace(n) => o.Trim(),
            _ => $"=== NATIVE TEXT ===\n{nativeText.Trim()}\n\n=== OCR TEXT ===\n{ocrText.Trim()}"
        };

        return new DocumentTextBundle(nativeText, string.IsNullOrWhiteSpace(ocrText) ? null : ocrText, merged, ocrUsed);
    }
}
