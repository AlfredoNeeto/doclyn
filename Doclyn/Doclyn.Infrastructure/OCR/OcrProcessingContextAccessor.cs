namespace Doclyn.Infrastructure.OCR;

public sealed class OcrProcessingContextAccessor
{
    public Func<int, CancellationToken, Task>? OnPageProcessedAsync { get; set; }

    public void Reset()
    {
        OnPageProcessedAsync = null;
    }
}
