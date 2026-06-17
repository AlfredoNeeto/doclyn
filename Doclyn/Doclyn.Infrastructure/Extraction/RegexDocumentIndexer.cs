using System.Globalization;
using System.Text.RegularExpressions;
using Doclyn.Application.Common.Interfaces;
using Doclyn.Application.Documents.Processing;
using Doclyn.Domain.Entities;
using Doclyn.Domain.Enums;

namespace Doclyn.Infrastructure.Extraction;

public sealed class RegexDocumentIndexer : IDocumentIndexer
{
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(5);

    public Dictionary<string, DocumentIndexerValue> ExtractIndexes(
        string text,
        IReadOnlyCollection<DocumentClassIndexer> indexers)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(indexers);

        var result = new Dictionary<string, DocumentIndexerValue>(StringComparer.OrdinalIgnoreCase);

        foreach (var indexer in indexers.Where(i => i.IsActive && !string.IsNullOrWhiteSpace(i.RegexPattern)))
        {
            var value = indexer.IsMultiple
                ? ExtractMany(indexer.RegexPattern!, text, indexer.DataType)
                : ExtractSingle(indexer.RegexPattern!, text, indexer.DataType);

            result[indexer.Name] = new DocumentIndexerValue(
                value,
                ExtractionSource.Regex.ToString(),
                1.0);
        }

        return result;
    }

    private static object? ExtractSingle(string pattern, string text, IndexerDataType dataType)
    {
        var regex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Multiline, RegexTimeout);
        var match = regex.Match(text);

        if (!match.Success)
            return null;

        var value = match.Groups.Count > 1 ? match.Groups[1].Value.Trim() : match.Value.Trim();
        return NormalizeValue(value, dataType);
    }

    private static object? ExtractMany(string pattern, string text, IndexerDataType dataType)
    {
        var regex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Multiline, RegexTimeout);
        var matches = regex.Matches(text)
            .Select(match => match.Groups.Count > 1 ? match.Groups[1].Value.Trim() : match.Value.Trim())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(value => NormalizeValue(value, dataType))
            .Where(value => value is not null)
            .ToList();

        return matches.Count == 0 ? null : matches.ToArray();
    }

    private static object? NormalizeValue(string value, IndexerDataType dataType)
    {
        return dataType switch
        {
            IndexerDataType.Currency => NormalizeCurrency(value),
            _ => value
        };
    }

    private static string? NormalizeCurrency(string value)
    {
        var normalized = value.Replace("R$", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace(".", string.Empty, StringComparison.Ordinal)
            .Replace(",", ".", StringComparison.Ordinal)
            .Trim();

        return decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount)
            ? amount.ToString("0.00", CultureInfo.InvariantCulture)
            : normalized;
    }
}
