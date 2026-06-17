using System.Text;
using Doclyn.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tesseract;

namespace Doclyn.Infrastructure.OCR;

public sealed class TesseractOcrService : IOcrService
{
    private readonly IPdfToImageConverter _pdfToImageConverter;
    private readonly OcrOptions _options;
    private readonly OcrProcessingContextAccessor _processingContextAccessor;
    private readonly ILogger<TesseractOcrService> _logger;

    public TesseractOcrService(
        IPdfToImageConverter pdfToImageConverter,
        IOptions<OcrOptions> options,
        OcrProcessingContextAccessor processingContextAccessor,
        ILogger<TesseractOcrService> logger)
    {
        _pdfToImageConverter = pdfToImageConverter;
        _options = options.Value;
        _processingContextAccessor = processingContextAccessor;
        _logger = logger;
    }

    public async Task<string> ExtractTextAsync(Stream pdfStream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pdfStream);

        var tessDataPath = ResolveTessDataPath(_options.TessDataPath);
        var pages = await _pdfToImageConverter.ConvertAsync(pdfStream, cancellationToken);
        var textBuilder = new StringBuilder();

        using var engine = new TesseractEngine(tessDataPath, _options.Language, EngineMode.Default);

        foreach (var page in pages)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var image = Pix.LoadFromMemory(page.ImageBytes);
            using var result = engine.Process(image);
            var pageText = result.GetText();

            if (!string.IsNullOrWhiteSpace(pageText))
            {
                if (textBuilder.Length > 0)
                {
                    textBuilder.AppendLine();
                }

                textBuilder.AppendLine(pageText.Trim());
            }

            if (_processingContextAccessor.OnPageProcessedAsync is not null)
            {
                await _processingContextAccessor.OnPageProcessedAsync(page.PageNumber, cancellationToken);
            }
        }

        return textBuilder.ToString().Trim();
    }

    private string ResolveTessDataPath(string configuredPath)
    {
        var path = configuredPath;

        if (!Path.IsPathRooted(path))
        {
            path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, path));
        }

        if (!Directory.Exists(path))
        {
            _logger.LogWarning("Configured tessdata path {TessDataPath} does not exist.", path);
        }

        return path;
    }
}
