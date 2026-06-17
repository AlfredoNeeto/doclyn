using MediatR;

namespace Doclyn.Application.Documents.ReprocessByFilter;

public sealed record ReprocessByFilterCommand(
    string? Status,
    string? DocumentType,
    DateTime? From,
    DateTime? To) : IRequest<ReprocessByFilterResponse>;
