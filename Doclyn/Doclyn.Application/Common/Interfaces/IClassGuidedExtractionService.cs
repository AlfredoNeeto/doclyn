using Doclyn.Application.DocumentExtraction.ClassGuidedExtraction;

namespace Doclyn.Application.Common.Interfaces;

public interface IClassGuidedExtractionService
{
    Task<ClassGuidedExtractionResult> ExtractAsync(
        Guid documentClassId,
        string documentText,
        CancellationToken cancellationToken = default);
}
