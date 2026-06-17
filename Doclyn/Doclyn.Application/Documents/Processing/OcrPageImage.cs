namespace Doclyn.Application.Documents.Processing;

public sealed record OcrPageImage(
    int PageNumber,
    byte[] ImageBytes);
