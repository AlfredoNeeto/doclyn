using Doclyn.Application.Documents.Processing;

namespace Doclyn.Application.Common.Interfaces;

public interface IPdfToImageConverter
{
    Task<IReadOnlyCollection<OcrPageImage>> ConvertAsync(
        Stream pdfStream,
        CancellationToken cancellationToken = default);
}
