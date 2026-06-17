using Doclyn.Application.Common.Interfaces;

namespace Doclyn.Infrastructure.OCR;

public sealed class NotImplementedOcrService : IOcrService
{
    public Task<string> ExtractTextAsync(Stream pdfStream, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(string.Empty);
    }
}
