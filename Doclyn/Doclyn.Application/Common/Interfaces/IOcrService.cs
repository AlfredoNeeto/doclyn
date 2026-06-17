namespace Doclyn.Application.Common.Interfaces;

public interface IOcrService
{
    Task<string> ExtractTextAsync(Stream pdfStream, CancellationToken cancellationToken = default);
}
