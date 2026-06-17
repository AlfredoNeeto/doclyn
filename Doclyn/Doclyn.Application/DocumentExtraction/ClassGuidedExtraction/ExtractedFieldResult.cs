using Doclyn.Domain.Enums;

namespace Doclyn.Application.DocumentExtraction.ClassGuidedExtraction;

public sealed record ExtractedFieldResult(
    object? Value,
    decimal Confidence,
    ExtractionSource Source,
    ValidationStatus ValidationStatus);
