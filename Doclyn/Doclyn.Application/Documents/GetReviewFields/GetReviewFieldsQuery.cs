using MediatR;

namespace Doclyn.Application.Documents.GetReviewFields;

public sealed record GetReviewFieldsQuery(Guid DocumentId) : IRequest<GetReviewFieldsResponse>;
