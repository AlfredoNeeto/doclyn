using Doclyn.Application.DocumentExtraction.ClassGuidedExtraction;
using Doclyn.Application.Documents.Processing;
using Doclyn.Domain.Enums;

namespace Doclyn.Infrastructure.AI;

public sealed class ExtractionMergeService
{
    public Dictionary<string, ExtractedFieldResult> MergeFields(
        IReadOnlyDictionary<string, DocumentIndexerValue> regexExtraction,
        IReadOnlyDictionary<string, object?>? aiExtraction)
    {
        var fields = new Dictionary<string, ExtractedFieldResult>(StringComparer.OrdinalIgnoreCase);

        if (aiExtraction is not null)
        {
            foreach (var pair in aiExtraction)
            {
                var confidence = 0.8m;
                var value = pair.Value;

                if (value is Dictionary<string, object?> nested
                    && nested.TryGetValue("confidence", out var confObj)
                    && confObj is decimal nestedConfidence)
                {
                    confidence = nestedConfidence;
                    value = nested.GetValueOrDefault("value");
                }

                fields[pair.Key] = new ExtractedFieldResult(
                    value,
                    confidence,
                    ExtractionSource.AI,
                    confidence >= 0.90m ? ValidationStatus.Validated
                        : confidence >= 0.70m ? ValidationStatus.NeedsReview
                        : ValidationStatus.Rejected);
            }
        }

        foreach (var pair in regexExtraction)
        {
            if (HasMeaningfulValue(pair.Value.Value))
            {
                fields[pair.Key] = new ExtractedFieldResult(
                    pair.Value.Value,
                    (decimal)pair.Value.Confidence,
                    MapSource(pair.Value.Source),
                    ValidationStatus.Validated);
            }
        }

        return fields;
    }

    private static ExtractionSource MapSource(string source)
    {
        return source switch
        {
            "AI" => ExtractionSource.AI,
            var s when s != null && s.StartsWith("Regex", StringComparison.OrdinalIgnoreCase) => ExtractionSource.Regex,
            "Manual" => ExtractionSource.Manual,
            _ => ExtractionSource.Merged
        };
    }

    public Dictionary<string, object?> Merge(
        DocumentClassificationResult classification,
        IReadOnlyDictionary<string, object?> regexExtraction,
        IReadOnlyDictionary<string, object?>? aiExtraction)
    {
        var finalResult = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        if (aiExtraction is not null)
        {
            foreach (var pair in aiExtraction)
            {
                finalResult[pair.Key] = pair.Value;
            }
        }

        foreach (var pair in regexExtraction)
        {
            if (HasMeaningfulValue(pair.Value))
            {
                finalResult[pair.Key] = pair.Value;
            }
        }

        finalResult["documentType"] = classification.DocumentType;
        finalResult["group"] = classification.Group;
        finalResult["subgroup"] = classification.Subgroup;
        finalResult["confidence"] = classification.Confidence;

        return finalResult;
    }

    private static bool HasMeaningfulValue(object? value)
    {
        return value switch
        {
            null => false,
            string text => !string.IsNullOrWhiteSpace(text),
            Array array => array.Length > 0,
            _ => true
        };
    }
}
