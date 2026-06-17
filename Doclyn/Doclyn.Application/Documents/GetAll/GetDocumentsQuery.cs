using MediatR;

namespace Doclyn.Application.Documents.GetAll;

public sealed record GetDocumentsQuery(
    int Page = 1,
    int PageSize = 10,
    string? Status = null,
    string? DocumentType = null,
    DateTime? From = null,
    DateTime? To = null,
    string? Search = null) : IRequest<GetDocumentsResponse>;
