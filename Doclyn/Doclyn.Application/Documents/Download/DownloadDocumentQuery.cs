using MediatR;

namespace Doclyn.Application.Documents.Download;

public sealed record DownloadDocumentQuery(Guid DocumentId) : IRequest<DownloadDocumentResponse>;
