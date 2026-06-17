namespace Doclyn.Infrastructure.OCR;

public static class OcrTextQualityEvaluator
{
    public static OcrQualityResult Evaluate(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new OcrQualityResult(0, 0, 0, 0, false, "empty");
        }

        var lines = text.Split('\n');
        var totalLines = lines.Length;
        var nonEmptyLines = lines.Count(l => !string.IsNullOrWhiteSpace(l));
        var alphaCount = 0;
        var digitCount = 0;
        var totalChars = 0;

        foreach (var c in text)
        {
            if (char.IsWhiteSpace(c))
                continue;
            totalChars++;

            if (char.IsLetter(c))
                alphaCount++;
            else if (char.IsDigit(c))
                digitCount++;
        }

        var alphaRatio = totalChars > 0 ? (double)alphaCount / totalChars : 0;
        var digitRatio = totalChars > 0 ? (double)digitCount / totalChars : 0;
        var looksUsable = totalChars >= 100
            && alphaRatio >= 0.3;

        var quality = looksUsable
            ? alphaRatio >= 0.6 && nonEmptyLines >= 5 ? "good" : "adequate"
            : "low";

        return new OcrQualityResult(totalLines, nonEmptyLines, alphaRatio, digitRatio, looksUsable, quality);
    }

    public static bool TextLooksSufficient(string? text, int minimumTextLength, string[] minimumKeywords)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        if (text.Length < minimumTextLength)
            return false;

        var quality = Evaluate(text);
        return quality.LooksUsable;
    }
}

public sealed record OcrQualityResult(
    int TotalLines,
    int NonEmptyLines,
    double AlphaRatio,
    double DigitRatio,
    bool LooksUsable,
    string Quality);
