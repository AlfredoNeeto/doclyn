namespace Doclyn.Application.DocumentClasses.GetTop;

public sealed record GetTopDocumentClassesResponse(
    Guid Id,
    string Name,
    string DisplayName,
    string Group,
    string SubGroup,
    int ExampleCount);
