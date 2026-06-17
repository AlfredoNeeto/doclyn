namespace Doclyn.Application.DocumentClasses.GetById;

public sealed record GetDocumentClassByIdResponse(
    Guid Id,
    string Name,
    string DisplayName,
    string Group,
    string SubGroup,
    string Description,
    bool IsSystemDefined,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
