namespace Doclyn.Application.Documents.GetAll;

public sealed record GetDocumentsResponse(
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages,
    IReadOnlyList<DocumentListItemResponse> Items);
