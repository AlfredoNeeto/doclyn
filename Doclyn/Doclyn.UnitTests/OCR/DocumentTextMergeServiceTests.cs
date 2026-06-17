using Doclyn.Infrastructure.OCR;
using Doclyn.Infrastructure.Processing;

namespace Doclyn.UnitTests.OCR;

public sealed class DocumentTextMergeServiceTests
{
    private readonly DocumentTextMergeService _service = new();

    [Fact]
    public void Merge_Should_Return_NativeText_When_OcrText_Is_Empty()
    {
        var result = _service.Merge("native content", null, false);

        Assert.Equal("native content", result.MergedText);
        Assert.Null(result.OcrText);
        Assert.False(result.OcrUsed);
    }

    [Fact]
    public void Merge_Should_Return_OcrText_When_NativeText_Is_Empty()
    {
        var result = _service.Merge("", "ocr content", true);

        Assert.Equal("ocr content", result.MergedText);
        Assert.Equal("ocr content", result.OcrText);
        Assert.True(result.OcrUsed);
    }

    [Fact]
    public void Merge_Should_Combine_Native_And_Ocr_With_Separators_When_Both_Exist()
    {
        var result = _service.Merge("native text", "ocr text", true);

        Assert.Contains("=== NATIVE TEXT ===", result.MergedText);
        Assert.Contains("=== OCR TEXT ===", result.MergedText);
        Assert.Contains("native text", result.MergedText);
        Assert.Contains("ocr text", result.MergedText);
        Assert.True(result.OcrUsed);
    }

    [Fact]
    public void Merge_Should_Return_Empty_When_Both_Are_Empty()
    {
        var result = _service.Merge("", null, false);

        Assert.Equal(string.Empty, result.MergedText);
        Assert.Null(result.OcrText);
        Assert.False(result.OcrUsed);
    }

    [Fact]
    public void Merge_Should_Trim_Texts()
    {
        var result = _service.Merge("  native  ", "  ocr  ", true);

        var lines = result.MergedText.Split('\n');
        Assert.Contains(lines, l => l == "  native  ".Trim());
        Assert.Contains(lines, l => l == "  ocr  ".Trim());
    }
}
