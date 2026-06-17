namespace Doclyn.Application.DocumentClasses.GetAll;

public sealed record GetDocumentClassesResponse(
    IReadOnlyList<DocumentClassListItemResponse> Items);
