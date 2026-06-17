using Doclyn.Application.DocumentExtraction.ClassGuidedExtraction;

namespace Doclyn.Application.Documents.Insights;

public sealed record ExtractedDocumentData(
    Guid DocumentId,
    Guid? DocumentClassId,
    string DocumentType,
    IReadOnlyDictionary<string, ExtractedFieldResult> Fields,
    string? DocumentText);
