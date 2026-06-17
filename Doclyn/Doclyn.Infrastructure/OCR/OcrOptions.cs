namespace Doclyn.Infrastructure.OCR;

public sealed class OcrOptions
{
    public const string Section = "Ocr";

    public bool Enabled { get; init; } = true;
    public string Language { get; init; } = "por";
    public string TessDataPath { get; init; } = "./tessdata";
    public int MinimumTextLength { get; init; } = 100;
    public int MaxPages { get; init; } = 20;
    public int Dpi { get; init; } = 300;
}
