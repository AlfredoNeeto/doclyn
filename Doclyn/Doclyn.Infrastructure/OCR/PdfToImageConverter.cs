using Doclyn.Application.Common.Interfaces;
using Doclyn.Application.Documents.Processing;
using Microsoft.Extensions.Options;
using PDFtoImage;
using SkiaSharp;

namespace Doclyn.Infrastructure.OCR;

public sealed class PdfToImageConverter : IPdfToImageConverter
{
    private readonly OcrOptions _options;

    public PdfToImageConverter(IOptions<OcrOptions> options)
    {
        _options = options.Value;
    }

    public async Task<IReadOnlyCollection<OcrPageImage>> ConvertAsync(
        Stream pdfStream,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pdfStream);

        using var memoryStream = new MemoryStream();
        if (pdfStream.CanSeek)
        {
            pdfStream.Position = 0;
        }

        await pdfStream.CopyToAsync(memoryStream, cancellationToken);
        var pdfBytes = memoryStream.ToArray();
        var images = Conversion.ToImages(pdfBytes, dpi: _options.Dpi).Take(_options.MaxPages);
        var pages = new List<OcrPageImage>();

        foreach (var (bitmap, index) in images.Select((bitmap, index) => (bitmap, index)))
        {
            using (bitmap)
            {
                using var data = bitmap.Encode(SKEncodedImageFormat.Png, 100);
                pages.Add(new OcrPageImage(index + 1, data.ToArray()));
            }
        }

        return pages;
    }
}
