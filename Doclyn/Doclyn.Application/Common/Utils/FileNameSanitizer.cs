using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Doclyn.Application.Common.Utils;

public static partial class FileNameSanitizer
{
    private const int MaxFileNameLength = 100;

    public static string SanitizeFileName(string fileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        var name = Path.GetFileNameWithoutExtension(fileName);

        // Remove acentos e converte para minúsculas.
        name = RemoveAccents(name).ToLowerInvariant();

        // Substitui espaços por hífen.
        name = name.Replace(' ', '-');

        // Mantém apenas letras, números, hífen e underscore.
        name = InvalidCharsRegex().Replace(name, string.Empty);

        // Remove hífens/underscores duplicados.
        name = DuplicateSeparatorsRegex().Replace(name, "-");

        // Remove hífen/underscore no início ou fim.
        name = name.Trim('-', '_');

        if (string.IsNullOrWhiteSpace(name))
        {
            name = "document";
        }

        if (name.Length > MaxFileNameLength)
        {
            name = name[..MaxFileNameLength].Trim('-', '_');
        }

        return $"{name}.pdf";
    }

    private static string RemoveAccents(string text)
    {
        var normalized = text.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(c);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    [GeneratedRegex(@"[^a-z0-9\-_]", RegexOptions.Compiled)]
    private static partial Regex InvalidCharsRegex();

    [GeneratedRegex(@"[-_]{2,}", RegexOptions.Compiled)]
    private static partial Regex DuplicateSeparatorsRegex();
}
