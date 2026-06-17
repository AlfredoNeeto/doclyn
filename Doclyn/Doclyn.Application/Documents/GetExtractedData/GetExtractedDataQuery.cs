using MediatR;

namespace Doclyn.Application.Documents.GetExtractedData;

public sealed record GetExtractedDataQuery(Guid DocumentId) : IRequest<GetExtractedDataResponse>;
