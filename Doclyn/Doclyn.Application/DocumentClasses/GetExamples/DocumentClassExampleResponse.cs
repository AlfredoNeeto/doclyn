namespace Doclyn.Application.DocumentClasses.GetExamples;

public sealed record DocumentClassExampleResponse(
    Guid Id,
    Guid DocumentId,
    string FileName,
    decimal Confidence,
    DateTime CreatedAt);
