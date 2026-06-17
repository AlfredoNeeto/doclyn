namespace Doclyn.Application.DocumentExtraction.ClassGuidedExtraction;

public sealed record ClassGuidedExtractionResult(
    Guid DocumentClassId,
    Dictionary<string, ExtractedFieldResult> Fields);
