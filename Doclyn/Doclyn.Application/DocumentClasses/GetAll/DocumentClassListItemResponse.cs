namespace Doclyn.Application.DocumentClasses.GetAll;

public sealed record DocumentClassListItemResponse(
    Guid Id,
    string Name,
    string DisplayName,
    string Group,
    string SubGroup,
    bool IsActive);
