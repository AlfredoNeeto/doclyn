using Doclyn.Application.Common.Interfaces;
using UglyToad.PdfPig;

namespace Doclyn.Infrastructure.PDF;

public sealed class PdfTextExtractor : IPdfTextExtractor
{
    public Task<string> ExtractTextAsync(Stream pdfStream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pdfStream);

        if (pdfStream.CanSeek)
        {
            pdfStream.Position = 0;
        }

        using var document = PdfDocument.Open(pdfStream);
        var text = string.Join(Environment.NewLine, document.GetPages().Select(page => page.Text));

        if (pdfStream.CanSeek)
        {
            pdfStream.Position = 0;
        }

        return Task.FromResult(text);
    }
}
